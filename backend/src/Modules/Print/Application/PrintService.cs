using System.Globalization;
using System.Net;
using System.Text;
using Ordevo.BuildingBlocks.Results;
using Ordevo.Modules.Print.Infrastructure;

namespace Ordevo.Modules.Print.Application;

public sealed class PrintService(IPrintRepository repo)
{
    private static readonly CultureInfo Tr = CultureInfo.GetCultureInfo("tr-TR");

    public async Task<Result<ReceiptDocumentDto>> ReceiptAsync(string tenantId, string branchId, string orderId, CancellationToken ct = default)
    {
        var header = await repo.GetReceiptHeaderAsync(tenantId, branchId, orderId, ct);
        if (header is null)
            return Error.NotFound("print.order.not_found", "Adisyon bulunamadı.");

        var lines = (await repo.GetReceiptLinesAsync(orderId, ct))
            .Select(x => new ReceiptLineDto(x.Name, x.Quantity, x.UnitPrice, x.LineTotal, x.Note))
            .ToList();
        var payments = (await repo.GetReceiptPaymentsAsync(tenantId, orderId, ct))
            .Select(x => new ReceiptPaymentDto(x.Method, x.Amount, x.TipAmount, x.Reference))
            .ToList();

        var lineTotal = lines.Sum(x => x.LineTotal);
        if (header.Total <= 0 && lineTotal > 0)
        {
            header.Subtotal = lineTotal;
            header.Total = Math.Max(0, lineTotal - header.DiscountTotal + header.TaxTotal);
        }

        var plain = BuildReceiptText(header, lines, payments);
        var html = BuildReceiptHtml(header, lines, payments);
        return new ReceiptDocumentDto(
            header.OrderId,
            header.OrderNo,
            header.TableName,
            header.OrderType,
            header.Status,
            header.InvoiceNo,
            header.Subtotal,
            header.DiscountTotal,
            header.TaxTotal,
            header.Total,
            header.OpenedAt,
            header.ClosedAt,
            lines,
            payments,
            plain,
            html);
    }

    public async Task<Result<KitchenTicketDocumentDto>> KitchenTicketAsync(string tenantId, string branchId, string orderId, CancellationToken ct = default)
    {
        var header = await repo.GetReceiptHeaderAsync(tenantId, branchId, orderId, ct);
        if (header is null)
            return Error.NotFound("print.order.not_found", "Adisyon bulunamadı.");

        var lines = (await repo.GetKitchenLinesAsync(orderId, ct))
            .Select(x => new KitchenTicketLineDto(x.Name, x.Quantity, x.CourseNo, x.Status, x.Station, x.Note, x.Modifiers))
            .ToList();

        var plain = BuildKitchenText(header, lines);
        var html = BuildKitchenHtml(header, lines);
        return new KitchenTicketDocumentDto(header.OrderId, header.OrderNo, header.TableName, header.OrderType, header.OpenedAt, lines, plain, html);
    }

    public async Task<Result<PrintJobDto>> QueueReceiptAsync(string tenantId, string branchId, string userId, string orderId, QueuePrintRequest request, CancellationToken ct = default)
    {
        var document = await ReceiptAsync(tenantId, branchId, orderId, ct);
        if (document.IsFailure)
            return document.Error;

        return Map(await repo.QueueAsync(tenantId, branchId, userId, "receipt", orderId, document.Value, Clean(request.TerminalId), NormalizeCopies(request.Copies), Clean(request.PrinterName), ct));
    }

    public async Task<Result<PrintJobDto>> QueueKitchenTicketAsync(string tenantId, string branchId, string userId, string orderId, QueuePrintRequest request, CancellationToken ct = default)
    {
        var document = await KitchenTicketAsync(tenantId, branchId, orderId, ct);
        if (document.IsFailure)
            return document.Error;

        return Map(await repo.QueueAsync(tenantId, branchId, userId, "kitchen_ticket", orderId, document.Value, Clean(request.TerminalId), NormalizeCopies(request.Copies), Clean(request.PrinterName), ct));
    }

    public async Task<Result<byte[]>> EscPosReceiptAsync(string tenantId, string branchId, string orderId, string businessName, int width, CancellationToken ct = default)
    {
        var document = await ReceiptAsync(tenantId, branchId, orderId, ct);
        if (document.IsFailure)
            return document.Error;
        return EscPosEncoder.Receipt(document.Value!, businessName, width);
    }

    public async Task<Result<byte[]>> EscPosKitchenTicketAsync(string tenantId, string branchId, string orderId, int width, CancellationToken ct = default)
    {
        var document = await KitchenTicketAsync(tenantId, branchId, orderId, ct);
        if (document.IsFailure)
            return document.Error;
        return EscPosEncoder.KitchenTicket(document.Value!, width);
    }

    public async Task<IReadOnlyList<PrintJobDto>> ListJobsAsync(string tenantId, string branchId, string? status, int take, CancellationToken ct = default)
        => (await repo.ListJobsAsync(tenantId, branchId, Clean(status), Math.Clamp(take, 1, 100), ct)).Select(Map).ToList();

    private static PrintJobDto Map(PrintJobRow x)
        => new(x.Id, x.BranchId, x.JobType, x.OrderId, x.TerminalId, x.Status, x.Copies, x.ErrorMessage, x.CreatedAt, x.UpdatedAt);

    private static int NormalizeCopies(int copies) => copies <= 0 ? 1 : Math.Clamp(copies, 1, 9);
    private static string? Clean(string? value) => string.IsNullOrWhiteSpace(value) ? null : value.Trim();

    private static string BuildReceiptText(ReceiptHeaderRow header, IReadOnlyList<ReceiptLineDto> lines, IReadOnlyList<ReceiptPaymentDto> payments)
    {
        var sb = new StringBuilder();
        sb.AppendLine("ORDEVO");
        sb.AppendLine($"Adisyon #{header.OrderNo}");
        sb.AppendLine($"Masa: {header.TableName ?? header.OrderType}");
        if (header.InvoiceNo is not null)
            sb.AppendLine($"Fis No: {header.InvoiceNo}");
        sb.AppendLine($"Acilis: {header.OpenedAt.LocalDateTime:dd.MM.yyyy HH:mm}");
        if (header.ClosedAt is not null)
            sb.AppendLine($"Kapanis: {header.ClosedAt.Value.LocalDateTime:dd.MM.yyyy HH:mm}");
        sb.AppendLine(new string('-', 42));
        foreach (var line in lines)
        {
            var qty = line.Quantity.ToString("0.###", Tr);
            sb.AppendLine(Clip($"{qty} x {line.Name}", 30).PadRight(30) + Money(line.LineTotal).PadLeft(12));
            if (!string.IsNullOrWhiteSpace(line.Note))
                sb.AppendLine("  Not: " + Clip(line.Note, 35));
        }
        sb.AppendLine(new string('-', 42));
        sb.AppendLine("Ara Toplam".PadRight(30) + Money(header.Subtotal).PadLeft(12));
        if (header.DiscountTotal > 0)
            sb.AppendLine("Indirim".PadRight(30) + ("-" + Money(header.DiscountTotal)).PadLeft(12));
        sb.AppendLine("KDV".PadRight(30) + Money(header.TaxTotal).PadLeft(12));
        sb.AppendLine("TOPLAM".PadRight(30) + Money(header.Total).PadLeft(12));
        if (payments.Count > 0)
        {
            sb.AppendLine(new string('-', 42));
            foreach (var payment in payments)
                sb.AppendLine(Clip(payment.Method, 30).PadRight(30) + Money(payment.Amount + payment.TipAmount).PadLeft(12));
        }
        sb.AppendLine(new string('-', 42));
        sb.AppendLine("Tesekkur ederiz");
        return sb.ToString();
    }

    private static string BuildKitchenText(ReceiptHeaderRow header, IReadOnlyList<KitchenTicketLineDto> lines)
    {
        var sb = new StringBuilder();
        sb.AppendLine($"SIPARIS FISI #{header.OrderNo}");
        sb.AppendLine($"Masa: {header.TableName ?? header.OrderType}");
        sb.AppendLine($"Saat: {header.OpenedAt.LocalDateTime:HH:mm}");
        sb.AppendLine(new string('-', 42));
        foreach (var line in lines.OrderBy(x => x.CourseNo).ThenBy(x => x.Station).ThenBy(x => x.Name))
        {
            sb.AppendLine($"{line.Quantity:0.###} x {line.Name}");
            if (!string.IsNullOrWhiteSpace(line.Modifiers))
                sb.AppendLine("  +" + line.Modifiers);
            if (!string.IsNullOrWhiteSpace(line.Note))
                sb.AppendLine("  NOT: " + line.Note);
            if (!string.IsNullOrWhiteSpace(line.Station))
                sb.AppendLine("  Istasyon: " + line.Station);
        }
        return sb.ToString();
    }

    private static string BuildReceiptHtml(ReceiptHeaderRow header, IReadOnlyList<ReceiptLineDto> lines, IReadOnlyList<ReceiptPaymentDto> payments)
    {
        var sb = new StringBuilder();
        sb.Append("<article class=\"print-doc receipt\"><h2>ORDEVO</h2>");
        sb.Append($"<p>Adisyon #{header.OrderNo} - {Html(header.TableName ?? header.OrderType)}</p><table><tbody>");
        foreach (var line in lines)
            sb.Append($"<tr><td>{line.Quantity:0.###} x {Html(line.Name)}</td><td>{Money(line.LineTotal)}</td></tr>");
        sb.Append("</tbody></table><dl>");
        sb.Append($"<dt>Ara Toplam</dt><dd>{Money(header.Subtotal)}</dd>");
        sb.Append($"<dt>Indirim</dt><dd>{Money(header.DiscountTotal)}</dd>");
        sb.Append($"<dt>KDV</dt><dd>{Money(header.TaxTotal)}</dd>");
        sb.Append($"<dt>Toplam</dt><dd>{Money(header.Total)}</dd>");
        sb.Append("</dl>");
        if (payments.Count > 0)
        {
            sb.Append("<h3>Odemeler</h3><table><tbody>");
            foreach (var payment in payments)
                sb.Append($"<tr><td>{Html(payment.Method)}</td><td>{Money(payment.Amount + payment.TipAmount)}</td></tr>");
            sb.Append("</tbody></table>");
        }
        sb.Append("</article>");
        return sb.ToString();
    }

    private static string BuildKitchenHtml(ReceiptHeaderRow header, IReadOnlyList<KitchenTicketLineDto> lines)
    {
        var sb = new StringBuilder();
        sb.Append($"<article class=\"print-doc kitchen\"><h2>Siparis Fisi #{header.OrderNo}</h2><p>{Html(header.TableName ?? header.OrderType)}</p><table><tbody>");
        foreach (var line in lines.OrderBy(x => x.CourseNo).ThenBy(x => x.Station).ThenBy(x => x.Name))
        {
            sb.Append($"<tr><td>{line.Quantity:0.###} x {Html(line.Name)}");
            if (!string.IsNullOrWhiteSpace(line.Note))
                sb.Append($"<br><small>{Html(line.Note)}</small>");
            sb.Append("</td><td>");
            sb.Append(Html(line.Station ?? line.Status));
            sb.Append("</td></tr>");
        }
        sb.Append("</tbody></table></article>");
        return sb.ToString();
    }

    private static string Money(decimal value) => value.ToString("C", Tr);
    private static string Html(string value) => WebUtility.HtmlEncode(value);
    private static string Clip(string value, int max) => value.Length <= max ? value : value[..max];
}
