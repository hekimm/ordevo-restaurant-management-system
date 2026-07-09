using System.Text.Json;
using Microsoft.Extensions.Logging;
using Ordevo.Modules.EInvoice.Application;

namespace Ordevo.Modules.EInvoice.Infrastructure;

public sealed class MockEInvoiceProvider(ILogger<MockEInvoiceProvider> logger) : IEInvoiceProvider
{
    public string Name => "mock";

    public Task<EInvoiceIssueResult> IssueAsync(EInvoiceDocumentModel document, CancellationToken ct = default)
    {
        var ettn = DeterministicGuid(document.DocumentId).ToString();
        var prefix = document.DocumentType == "efatura" ? "GIB" : "ARS";
        var number = $"{prefix}{document.IssuedAt:yyyy}{document.OrderNo:D9}";

        var raw = JsonSerializer.Serialize(new
        {
            provider = Name,
            accepted = true,
            ettn,
            invoiceNumber = number,
            documentType = document.DocumentType,
            scenario = document.Scenario,
            grandTotal = document.GrandTotal,
            note = "Simüle edilmiş entegratör yanıtı — gerçek entegratör takılınca değişir."
        });

        logger.LogInformation("MockEInvoice issued {Type} {Number} (ETTN {Ettn}) for order #{OrderNo}, total {Total}",
            document.DocumentType, number, ettn, document.OrderNo, document.GrandTotal);

        return Task.FromResult(new EInvoiceIssueResult(
            Success: true,
            Status: "accepted",
            ExternalId: ettn,
            Uuid: ettn,
            InvoiceNumber: number,
            PdfUrl: null,
            RawResponseJson: raw,
            Error: null));
    }

    public Task<EInvoiceStatusResult> GetStatusAsync(string externalId, CancellationToken ct = default)
        => Task.FromResult(new EInvoiceStatusResult(true, "accepted",
            JsonSerializer.Serialize(new { externalId, status = "accepted" }), null));

    public Task<EInvoiceCancelResult> CancelAsync(string externalId, string reason, CancellationToken ct = default)
        => Task.FromResult(new EInvoiceCancelResult(true, "cancelled",
            JsonSerializer.Serialize(new { externalId, status = "cancelled", reason }), null));

    private static Guid DeterministicGuid(string input)
    {
        var bytes = System.Security.Cryptography.MD5.HashData(System.Text.Encoding.UTF8.GetBytes(input));
        return new Guid(bytes);
    }
}
