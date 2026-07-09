using System.Text.Json;
using Microsoft.AspNetCore.Mvc;
using Ordevo.Web.Api;
using Ordevo.Web.Models;

namespace Ordevo.Web.Pages.Print;

public sealed class IndexModel(OrdevoApiClient api) : AppPageModel(api)
{
    [BindProperty(SupportsGet = true)]
    public string? OrderId { get; set; }

    [BindProperty]
    public QueueInput Queue { get; set; } = new();

    public ReceiptDocumentDto? Receipt { get; private set; }
    public KitchenTicketDocumentDto? KitchenTicket { get; private set; }
    public IReadOnlyList<PrintJobDto> Jobs { get; private set; } = [];
    public IReadOnlyList<TerminalDto> Printers { get; private set; } = [];
    public IReadOnlyList<TerminalCommandDto> Commands { get; private set; } = [];

    [BindProperty]
    public PrinterInput Printer { get; set; } = new();

    public int ActivePrinterCount => Printers.Count(x => x.IsActive == 1);
    public int OnlinePrinterCount => Printers.Count(IsOnline);
    public int WaitingJobCount => Jobs.Count(x => x.Status is "queued" or "sent");
    public int FailedJobCount => Jobs.Count(x => x.Status == "failed");
    public string? DefaultReceiptTerminalId => Printers.FirstOrDefault(x => IsRole(x, "receipt"))?.Id ?? Printers.FirstOrDefault(x => x.IsActive == 1)?.Id;
    public string? DefaultKitchenTerminalId => Printers.FirstOrDefault(x => IsRole(x, "kitchen"))?.Id ?? Printers.FirstOrDefault(x => x.IsActive == 1)?.Id;

    public async Task OnGetAsync(CancellationToken ct)
    {
        await LoadAsync(ct);
    }

    public async Task<IActionResult> OnPostReceiptAsync(CancellationToken ct)
    {
        if (string.IsNullOrWhiteSpace(OrderId))
        {
            Errors.Add("Adisyon seçilmedi.");
            await LoadAsync(ct);
            return Page();
        }

        var result = await Api.PostAsync<PrintJobDto>($"/api/print/orders/{Uri.EscapeDataString(OrderId)}/receipt/queue", new
        {
            Queue.TerminalId,
            Queue.Copies,
            Queue.PrinterName
        }, ct);

        if (result.IsSuccess)
        {
            NotifySuccess("Adisyon yazdırma kuyruğuna alındı.");
            return RedirectToPage(new { orderId = OrderId });
        }

        Errors.Add(UiFormat.Error(result));
        await LoadAsync(ct);
        return Page();
    }

    public async Task<IActionResult> OnPostKitchenAsync(CancellationToken ct)
    {
        if (string.IsNullOrWhiteSpace(OrderId))
        {
            Errors.Add("Adisyon seçilmedi.");
            await LoadAsync(ct);
            return Page();
        }

        var result = await Api.PostAsync<PrintJobDto>($"/api/print/orders/{Uri.EscapeDataString(OrderId)}/kitchen-ticket/queue", new
        {
            Queue.TerminalId,
            Queue.Copies,
            Queue.PrinterName
        }, ct);

        if (result.IsSuccess)
        {
            NotifySuccess("Mutfak fişi yazdırma kuyruğuna alındı.");
            return RedirectToPage(new { orderId = OrderId });
        }

        Errors.Add(UiFormat.Error(result));
        await LoadAsync(ct);
        return Page();
    }

    public async Task<IActionResult> OnPostPrinterAsync(CancellationToken ct)
    {
        if (Printer.ConnectionMode == "ethernet" && string.IsNullOrWhiteSpace(Printer.IpAddress))
            Errors.Add("Ağdaki yazıcı için cihaz adresi gerekli.");
        if (Printer.ConnectionMode == "serial" && string.IsNullOrWhiteSpace(Printer.SerialPath))
            Errors.Add("Seri bağlantı için port yolu gerekli.");

        if (Errors.Count > 0)
        {
            await LoadAsync(ct);
            return Page();
        }

        var settings = JsonSerializer.Serialize(new
        {
            role = Clean(Printer.Role) ?? "receipt",
            paperWidth = Printer.PaperWidth <= 0 ? 80 : Printer.PaperWidth,
            autoCut = Printer.AutoCut,
            cashDrawer = Printer.CashDrawer,
            profile = Clean(Printer.Profile) ?? "escpos"
        });

        var result = await Api.PostAsync<TerminalDto>("/api/integrations/terminals", new
        {
            name = Clean(Printer.Name) ?? DefaultPrinterName(Printer.Role),
            terminalType = "kitchen_printer",
            connectionMode = Clean(Printer.ConnectionMode) ?? "usb",
            ipAddress = Clean(Printer.IpAddress),
            port = Printer.ConnectionMode == "ethernet" ? Printer.Port : null,
            serialPath = Clean(Printer.SerialPath),
            providerTerminalId = Clean(Printer.ProviderTerminalId),
            settings
        }, ct);

        if (result.IsSuccess)
        {
            NotifySuccess("Yazıcı kaydedildi.");
            return RedirectToPage();
        }

        Errors.Add(UiFormat.Error(result));
        await LoadAsync(ct);
        return Page();
    }

    public async Task<IActionResult> OnPostTestAsync(string terminalId, CancellationToken ct)
    {
        if (string.IsNullOrWhiteSpace(terminalId))
        {
            Errors.Add("Test için yazıcı seçilmedi.");
            await LoadAsync(ct);
            return Page();
        }

        var payload = JsonSerializer.Serialize(new
        {
            kind = "test_print",
            title = "ORDEVO PRINTER TEST",
            lines = new[] { "Yazici baglantisi hazir.", DateTimeOffset.Now.ToString("dd.MM.yyyy HH:mm") }
        });

        var result = await Api.PostAsync<JsonElement>("/api/integrations/terminal-commands", new
        {
            commandType = "print",
            terminalId,
            payload,
            idempotencyKey = $"printer-test:{terminalId}:{DateTimeOffset.UtcNow.ToUnixTimeMilliseconds()}"
        }, ct);

        if (result.IsSuccess)
        {
            NotifySuccess("Test fişi kuyruğa alındı.");
            return RedirectToPage();
        }

        Errors.Add(UiFormat.Error(result));
        await LoadAsync(ct);
        return Page();
    }

    private async Task LoadAsync(CancellationToken ct)
    {
        Queue.Copies = Queue.Copies <= 0 ? 1 : Queue.Copies;

        if (!string.IsNullOrWhiteSpace(OrderId))
        {
            var id = Uri.EscapeDataString(OrderId.Trim());
            Receipt = await GetOneAsync<ReceiptDocumentDto>($"/api/print/orders/{id}/receipt", ct);
            KitchenTicket = await GetOneAsync<KitchenTicketDocumentDto>($"/api/print/orders/{id}/kitchen-ticket", ct);
        }

        Jobs = await GetListAsync<PrintJobDto>("/api/print/jobs?take=25", ct);
        var terminals = await GetListAsync<TerminalDto>("/api/integrations/terminals", ct);
        Printers = terminals
            .Where(x => x.TerminalType is "kitchen_printer" or "fiscal")
            .OrderByDescending(x => x.IsActive)
            .ThenBy(x => PrinterRole(x))
            .ThenBy(x => x.Name)
            .ToList();
        Commands = (await GetListAsync<TerminalCommandDto>("/api/integrations/terminal-commands?take=25", ct))
            .Where(x => x.CommandType == "print")
            .ToList();
    }

    public string PrinterName(string? terminalId)
        => string.IsNullOrWhiteSpace(terminalId)
            ? "-"
            : Printers.FirstOrDefault(x => x.Id == terminalId)?.Name ?? UiFormat.ShortId(terminalId);

    public static bool IsOnline(TerminalDto printer)
        => printer.IsActive == 1 && printer.LastSeenAt is not null && printer.LastSeenAt.Value >= DateTimeOffset.UtcNow.AddMinutes(-10);

    public static string PrinterStatus(TerminalDto printer)
    {
        if (printer.IsActive != 1)
            return "Kapalı";
        if (printer.LastSeenAt is null)
            return "Kurulum bekliyor";
        return IsOnline(printer) ? "Bağlı" : "Bağlantı yok";
    }

    public static string PrinterStatusClass(TerminalDto printer)
    {
        if (printer.IsActive != 1)
            return "off";
        if (IsOnline(printer))
            return "online";
        if (printer.LastSeenAt is null)
            return "setup";
        return "offline";
    }

    public static string LastSeen(TerminalDto printer)
        => printer.LastSeenAt is null ? "Henüz sinyal yok" : printer.LastSeenAt.Value.LocalDateTime.ToString("dd.MM HH:mm");

    public static string PrintStatusLabel(string status)
        => status switch
        {
            "queued" => "Bekliyor",
            "sent" => "Gönderildi",
            "printed" => "Yazdırıldı",
            "failed" => "Sorun var",
            "cancelled" => "İptal",
            _ => status
        };

    public static string PrintStatusClass(string status)
        => status switch
        {
            "printed" => "online",
            "failed" => "offline",
            "cancelled" => "off",
            _ => "setup"
        };

    public static string ConnectionLabel(TerminalDto printer)
        => printer.ConnectionMode switch
        {
            "usb" => "USB",
            "serial" => "Seri",
            "ethernet" => "Ethernet",
            "cloud" => "Bulut ajan",
            "app_to_app" => "Uygulama",
            _ => "Özel"
        };

    public static string EndpointLabel(TerminalDto printer)
        => printer.ConnectionMode switch
        {
            "ethernet" => string.IsNullOrWhiteSpace(printer.IpAddress) ? "IP bekliyor" : $"{printer.IpAddress}:{printer.Port ?? 9100}",
            "serial" => string.IsNullOrWhiteSpace(printer.SerialPath) ? "Port bekliyor" : printer.SerialPath,
            "usb" => Clean(printer.ProviderTerminalId) ?? "Yerel USB",
            _ => Clean(printer.ProviderTerminalId) ?? Clean(printer.DeviceId) ?? "Yerel ajan"
        };

    public static string PrinterRole(TerminalDto printer)
        => RoleValue(printer) switch
        {
            "receipt" => "Kasa fişi",
            "kitchen" => "Mutfak",
            "backup" => "Yedek",
            _ => printer.TerminalType == "fiscal" ? "Mali yazıcı" : "Yazıcı"
        };

    public static bool IsRole(TerminalDto printer, string role)
        => string.Equals(RoleValue(printer), role, StringComparison.OrdinalIgnoreCase);

    private static string? RoleValue(TerminalDto printer)
        => Setting(printer, "role") ?? Setting(printer, "printerRole");

    private static string? Setting(TerminalDto printer, string key)
    {
        if (string.IsNullOrWhiteSpace(printer.Settings))
            return null;

        try
        {
            using var doc = JsonDocument.Parse(printer.Settings);
            return doc.RootElement.TryGetProperty(key, out var value) ? value.ToString() : null;
        }
        catch (JsonException)
        {
            return null;
        }
    }

    private static string? Clean(string? value)
        => string.IsNullOrWhiteSpace(value) ? null : value.Trim();

    private static string DefaultPrinterName(string? role)
        => role switch
        {
            "kitchen" => "Mutfak Yazıcısı",
            "backup" => "Yedek Yazıcı",
            _ => "Kasa Yazıcısı"
        };

    public sealed class QueueInput
    {
        public string? TerminalId { get; set; }
        public int Copies { get; set; } = 1;
        public string? PrinterName { get; set; }
    }

    public sealed class PrinterInput
    {
        public string Name { get; set; } = "Kasa Yazıcısı";
        public string Role { get; set; } = "receipt";
        public string ConnectionMode { get; set; } = "usb";
        public string? IpAddress { get; set; }
        public int? Port { get; set; } = 9100;
        public string? SerialPath { get; set; }
        public string? ProviderTerminalId { get; set; }
        public int PaperWidth { get; set; } = 80;
        public string Profile { get; set; } = "escpos";
        public bool AutoCut { get; set; } = true;
        public bool CashDrawer { get; set; }
    }
}
