using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;
using Microsoft.AspNetCore.DataProtection;
using Microsoft.AspNetCore.Mvc;
using Ordevo.Web.Api;
using Ordevo.Web.Models;

namespace Ordevo.Web.Pages.Settings;

public sealed class IndexModel(
    OrdevoApiClient api,
    IConfiguration configuration,
    IDataProtectionProvider dataProtection) : AppPageModel(api)
{
    private const string DeveloperCookieName = "ordevo.dev-area";
    private readonly IDataProtector _protector = dataProtection.CreateProtector("Ordevo.Web.DeveloperArea.v1");

    [BindProperty(SupportsGet = true)]
    public string Tab { get; set; } = "developer";

    [BindProperty]
    public UnlockInput Unlock { get; set; } = new();

    [BindProperty]
    public ToggleForm Toggles { get; set; } = new();

    [BindProperty]
    public FiscalConnectorForm FiscalConnector { get; set; } = new();

    [BindProperty]
    public FiscalTerminalForm FiscalTerminal { get; set; } = new();

    [BindProperty]
    public FiscalTestSaleForm FiscalTestSale { get; set; } = new();

    public bool DeveloperUnlocked { get; private set; }
    public DeveloperSettingsDto DeveloperSettings { get; private set; } = new([], []);
    public IReadOnlyList<ConnectorDto> Connectors { get; private set; } = [];
    public IReadOnlyList<TerminalDto> Terminals { get; private set; } = [];
    public IReadOnlyList<IntegrationEventDto> Events { get; private set; } = [];
    public IReadOnlyList<TerminalCommandDto> Commands { get; private set; } = [];
    public IReadOnlyList<SyncEntityDto> Entities { get; private set; } = [];
    public IReadOnlyList<PendingMutationDto> PendingMutations { get; private set; } = [];
    public FiscalOverviewDto FiscalOverview { get; private set; } = new("kapalı", "disabled", false, 0, 0, [], []);

    public async Task OnGetAsync(CancellationToken ct)
    {
        await LoadAsync(ct);
    }

    public async Task<IActionResult> OnPostUnlockAsync(CancellationToken ct)
    {
        if (!PasswordMatches(Unlock.Password))
        {
            Errors.Add("Geliştirici parolası hatalı.");
            await LoadAsync(ct);
            return Page();
        }

        SetDeveloperCookie();
        NotifySuccess("Geliştirici alanı açıldı.");
        return RedirectToPage(new { tab = string.IsNullOrWhiteSpace(Tab) ? "developer" : Tab });
    }

    public IActionResult OnPostLock()
    {
        Response.Cookies.Delete(DeveloperCookieName);
        NotifySuccess("Geliştirici alanı kilitlendi.");
        return RedirectToPage(new { tab = "developer" });
    }

    public async Task<IActionResult> OnPostDeveloperTogglesAsync(CancellationToken ct)
    {
        if (!IsDeveloperUnlocked())
        {
            Errors.Add("Bu alanı değiştirmek için geliştirici parolası gerekli.");
            await LoadAsync(ct);
            return Page();
        }

        var result = await Api.PutAsync<DeveloperSettingsDto>("/api/settings/developer", new
        {
            modules = Toggles.Modules.ToDictionary(x => x.Code, x => x.IsEnabled),
            integrations = Toggles.Integrations.ToDictionary(x => x.Code, x => x.IsEnabled)
        }, ct);

        if (result.IsSuccess)
        {
            NotifySuccess("Geliştirici ayarları güncellendi.");
            return RedirectToPage(new { tab = "developer" });
        }

        Errors.Add(UiFormat.Error(result));
        await LoadAsync(ct);
        return Page();
    }

    public async Task<IActionResult> OnPostFiscalConnectorAsync(CancellationToken ct)
    {
        var code = string.IsNullOrWhiteSpace(FiscalConnector.Code) ? "fiscal-main" : FiscalConnector.Code.Trim();
        var result = await Api.PostAsync<ConnectorDto>("/api/integrations/connectors", new
        {
            code,
            name = string.IsNullOrWhiteSpace(FiscalConnector.Name) ? "Mali entegrasyon" : FiscalConnector.Name.Trim(),
            connectorType = FiscalConnector.ConnectorType,
            providerCode = string.IsNullOrWhiteSpace(FiscalConnector.ProviderCode) ? "gmp3-agent" : FiscalConnector.ProviderCode.Trim(),
            baseUrl = string.IsNullOrWhiteSpace(FiscalConnector.BaseUrl) ? null : FiscalConnector.BaseUrl.Trim(),
            authType = FiscalConnector.AuthType,
            secretRef = string.IsNullOrWhiteSpace(FiscalConnector.SecretRef) ? null : FiscalConnector.SecretRef.Trim(),
            settings = "{}"
        }, ct);

        if (result.IsSuccess)
        {
            NotifySuccess("Mali connector eklendi.");
            return RedirectToPage(new { tab = "fiscal" });
        }

        AddApiError(result);
        await LoadAsync(ct);
        return Page();
    }

    public async Task<IActionResult> OnPostFiscalTerminalAsync(CancellationToken ct)
    {
        var result = await Api.PostAsync<TerminalDto>("/api/integrations/terminals", new
        {
            name = string.IsNullOrWhiteSpace(FiscalTerminal.Name) ? "POS Cihazı" : FiscalTerminal.Name.Trim(),
            terminalType = FiscalTerminal.TerminalType,
            connectorId = string.IsNullOrWhiteSpace(FiscalTerminal.ConnectorId) ? null : FiscalTerminal.ConnectorId,
            providerTerminalId = string.IsNullOrWhiteSpace(FiscalTerminal.ProviderTerminalId) ? null : FiscalTerminal.ProviderTerminalId.Trim(),
            connectionMode = FiscalTerminal.ConnectionMode,
            ipAddress = string.IsNullOrWhiteSpace(FiscalTerminal.IpAddress) ? null : FiscalTerminal.IpAddress.Trim(),
            port = FiscalTerminal.Port,
            serialPath = string.IsNullOrWhiteSpace(FiscalTerminal.SerialPath) ? null : FiscalTerminal.SerialPath.Trim(),
            settings = "{}"
        }, ct);

        if (result.IsSuccess)
        {
            NotifySuccess("POS cihazı kaydedildi.");
            return RedirectToPage(new { tab = "fiscal" });
        }

        AddApiError(result);
        await LoadAsync(ct);
        return Page();
    }

    public async Task<IActionResult> OnPostFiscalTestSaleAsync(CancellationToken ct)
    {
        if (string.IsNullOrWhiteSpace(FiscalTestSale.TerminalId))
        {
            AddUiError("Test için POS cihazı seçin.");
            await LoadAsync(ct);
            Tab = "fiscal";
            return Page();
        }

        var result = await Api.PostAsync<FiscalPaymentResultDto>(
            $"/api/fiscal/terminals/{Uri.EscapeDataString(FiscalTestSale.TerminalId)}/test-sale",
            new { amount = FiscalTestSale.Amount <= 0 ? 0.01m : FiscalTestSale.Amount, currency = "TRY" },
            ct);

        if (result.IsSuccess)
        {
            NotifySuccess(result.Value?.UserMessage ?? "Cihaz test işlemi başarılı.");
            return RedirectToPage(new { tab = "fiscal" });
        }

        AddApiError(result);
        await LoadAsync(ct);
        Tab = "fiscal";
        return Page();
    }

    private async Task LoadAsync(CancellationToken ct)
    {
        Tab = string.IsNullOrWhiteSpace(Tab) ? "developer" : Tab;
        DeveloperUnlocked = IsDeveloperUnlocked();

        DeveloperSettings = await GetOneAsync<DeveloperSettingsDto>(
            DeveloperUnlocked ? "/api/settings/developer" : "/api/settings/runtime",
            ct) ?? new([], []);
        Toggles = ToggleForm.From(DeveloperSettings);

        if (!DeveloperUnlocked)
            return;

        Connectors = await GetListAsync<ConnectorDto>("/api/integrations/connectors", ct);
        Terminals = await GetListAsync<TerminalDto>("/api/integrations/terminals", ct);
        Events = await GetListAsync<IntegrationEventDto>("/api/integrations/events?take=50", ct);
        Commands = await GetListAsync<TerminalCommandDto>("/api/integrations/terminal-commands?take=50", ct);
        var fiscal = await Api.GetAsync<FiscalOverviewDto>("/api/fiscal/overview", ct);
        if (fiscal.IsSuccess && fiscal.Value is not null)
        {
            FiscalOverview = fiscal.Value;
        }
        else if (string.Equals(Tab, "fiscal", StringComparison.OrdinalIgnoreCase))
        {
            AddUiError("Mali entegrasyon tabloları henüz hazır değil. V22 migration uygulandıktan sonra bu ekran aktif olur.");
        }
        Entities = await GetListAsync<SyncEntityDto>("/api/sync/entities", ct);
        PendingMutations = await GetListAsync<PendingMutationDto>("/api/sync/mutations/pending?take=100", ct);
    }

    private bool PasswordMatches(string? candidate)
    {
        var expected = configuration["DeveloperArea:Password"];
        if (string.IsNullOrWhiteSpace(expected))
            expected = "ordevo-dev";

        var left = Encoding.UTF8.GetBytes(candidate ?? "");
        var right = Encoding.UTF8.GetBytes(expected);
        return left.Length == right.Length && CryptographicOperations.FixedTimeEquals(left, right);
    }

    private void SetDeveloperCookie()
    {
        var expires = DateTimeOffset.UtcNow.AddHours(8);
        var payload = $"{CurrentUserId()}|{expires:O}";
        Response.Cookies.Append(DeveloperCookieName, _protector.Protect(payload), new CookieOptions
        {
            HttpOnly = true,
            SameSite = SameSiteMode.Strict,
            Secure = Request.IsHttps,
            Expires = expires
        });
    }

    private bool IsDeveloperUnlocked()
    {
        if (!Request.Cookies.TryGetValue(DeveloperCookieName, out var cookie) || string.IsNullOrWhiteSpace(cookie))
            return false;

        try
        {
            var payload = _protector.Unprotect(cookie);
            var parts = payload.Split('|', 2);
            if (parts.Length != 2 || !string.Equals(parts[0], CurrentUserId(), StringComparison.Ordinal))
                return false;

            return DateTimeOffset.TryParse(parts[1], out var expires) && expires > DateTimeOffset.UtcNow;
        }
        catch
        {
            return false;
        }
    }

    private string CurrentUserId() => User.FindFirstValue(ClaimTypes.NameIdentifier) ?? "anonymous";

    public sealed class UnlockInput
    {
        public string Password { get; set; } = "";
    }

    public sealed class ToggleForm
    {
        public List<ToggleInput> Modules { get; set; } = [];
        public List<ToggleInput> Integrations { get; set; } = [];

        public static ToggleForm From(DeveloperSettingsDto settings) => new()
        {
            Modules = [.. settings.Modules.Select(ToggleInput.From)],
            Integrations = [.. settings.Integrations.Select(ToggleInput.From)]
        };
    }

    public sealed class ToggleInput
    {
        public string Code { get; set; } = "";
        public string Name { get; set; } = "";
        public string Description { get; set; } = "";
        public string Route { get; set; } = "";
        public bool IsEnabled { get; set; }

        public static ToggleInput From(DeveloperToggleDto toggle) => new()
        {
            Code = toggle.Code,
            Name = toggle.Name,
            Description = toggle.Description,
            Route = toggle.Route,
            IsEnabled = toggle.IsEnabled
        };
    }

    public sealed class FiscalConnectorForm
    {
        public string Code { get; set; } = "fiscal-main";
        public string Name { get; set; } = "Mali entegrasyon";
        public string ConnectorType { get; set; } = "payment_terminal";
        public string ProviderCode { get; set; } = "gmp3-agent";
        public string BaseUrl { get; set; } = "";
        public string AuthType { get; set; } = "none";
        public string SecretRef { get; set; } = "";
    }

    public sealed class FiscalTerminalForm
    {
        public string Name { get; set; } = "Ana POS";
        public string TerminalType { get; set; } = "payment";
        public string ConnectorId { get; set; } = "";
        public string ProviderTerminalId { get; set; } = "";
        public string ConnectionMode { get; set; } = "cloud";
        public string IpAddress { get; set; } = "";
        public int? Port { get; set; }
        public string SerialPath { get; set; } = "";
    }

    public sealed class FiscalTestSaleForm
    {
        public string TerminalId { get; set; } = "";
        public decimal Amount { get; set; } = 0.01m;
    }
}
