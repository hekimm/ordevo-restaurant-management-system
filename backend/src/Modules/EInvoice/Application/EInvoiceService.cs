using System.Text.Json;
using Microsoft.Extensions.Logging;
using Ordevo.BuildingBlocks.Results;
using Ordevo.Modules.EInvoice.Infrastructure;

namespace Ordevo.Modules.EInvoice.Application;

public sealed class EInvoiceService(IEInvoiceRepository repo, IEInvoiceProvider provider, ILogger<EInvoiceService> logger)
{
    private static readonly JsonSerializerOptions Json = new(JsonSerializerDefaults.Web);

    public async Task<Result<EInvoiceDocumentDto>> IssueForOrderAsync(
        string tenantId, string? userId, string orderId, IssueEInvoiceRequest request, CancellationToken ct = default)
    {
        var header = await repo.GetOrderHeaderAsync(tenantId, orderId, ct);
        if (header is null)
            return Error.NotFound("einvoice.order", "Sipariş bulunamadı.");
        if (!string.Equals(header.Status, "closed", StringComparison.OrdinalIgnoreCase))
            return Error.Validation("einvoice.order.status", "Yalnızca kapanmış (ödenmiş) siparişe fatura kesilebilir.");

        var lines = await repo.GetOrderLinesAsync(tenantId, orderId, ct);
        if (lines.Count == 0)
            return Error.Validation("einvoice.order.empty", "Faturalanacak kalem yok.");

        var buyerTaxNo = Clean(request.BuyerTaxNumber);
        var documentType = (Clean(request.DocumentType) ?? (buyerTaxNo is null ? "earsiv" : "efatura")).ToLowerInvariant();
        if (documentType is not ("efatura" or "earsiv"))
            return Error.Validation("einvoice.type", "Belge tipi efatura veya earsiv olmalı.");
        if (documentType == "efatura" && buyerTaxNo is null)
            return Error.Validation("einvoice.buyer.taxno", "e-Fatura için alıcı VKN/TCKN zorunludur.");

        var scenario = documentType == "efatura" ? "TICARIFATURA" : "EARSIVFATURA";
        var currency = string.IsNullOrWhiteSpace(header.Currency) ? "TRY" : header.Currency;

        var docLines = new List<EInvoiceLine>(lines.Count);
        decimal netSum = 0m, vatSum = 0m;
        foreach (var l in lines)
        {
            var gross = l.LineTotal;
            var rate = l.VatRate;
            var net = rate > 0 ? Math.Round(gross / (1 + rate / 100m), 4) : gross;
            var vat = Math.Round(gross - net, 4);
            netSum += net;
            vatSum += vat;
            docLines.Add(new EInvoiceLine(l.Name, l.Quantity, "C62", l.UnitPrice, rate, net, vat, gross));
        }

        var seller = new EInvoiceParty(
            header.BranchName is null ? header.SellerName : $"{header.SellerName} — {header.BranchName}",
            null, null, null, null, "Türkiye", null, null);
        var buyer = new EInvoiceParty(
            string.IsNullOrWhiteSpace(request.BuyerName) ? "Nihai Tüketici" : request.BuyerName!.Trim(),
            buyerTaxNo, Clean(request.BuyerTaxOffice), Clean(request.BuyerAddress),
            Clean(request.BuyerCity), "Türkiye", Clean(request.BuyerEmail), null);

        var model = new EInvoiceDocumentModel(
            DocumentId: Guid.NewGuid().ToString(),
            DocumentType: documentType,
            Scenario: scenario,
            Currency: currency,
            IssuedAt: header.ClosedAt ?? DateTimeOffset.UtcNow,
            OrderNo: header.OrderNo,
            Seller: seller,
            Buyer: buyer,
            Lines: docLines,
            Subtotal: Math.Round(netSum, 4),
            TaxTotal: Math.Round(vatSum, 4),
            GrandTotal: header.Total,
            PricesIncludeVat: true,
            Notes: Clean(request.Notes));

        var requestJson = JsonSerializer.Serialize(model, Json);
        var id = await repo.InsertDraftAsync(tenantId, header.BranchId, orderId, documentType, provider.Name,
            scenario, currency, model.Subtotal, model.TaxTotal, model.GrandTotal, buyer.Name, buyerTaxNo, requestJson, userId, ct);

        EInvoiceIssueResult result;
        try
        {
            result = await provider.IssueAsync(model, ct);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "EInvoice provider failed for order {OrderId}", orderId);
            const string message = "e-Belge sağlayıcısından yanıt alınamadı. Belge daha sonra tekrar gönderilebilir.";
            await repo.UpdateOutcomeAsync(tenantId, id, "error", null, null, null, null, null, message, false, ct);
            return Error.Failure("einvoice.provider", message);
        }

        await repo.UpdateOutcomeAsync(tenantId, id,
            result.Success ? result.Status : "error",
            result.ExternalId, result.Uuid, result.InvoiceNumber, result.PdfUrl,
            result.RawResponseJson, result.Error, markIssued: result.Success, ct);

        var row = await repo.GetAsync(tenantId, id, ct);
        return row is null ? Error.Failure("einvoice.persist", "Belge kaydedilemedi.") : Map(row);
    }

    public async Task<IReadOnlyList<EInvoiceDocumentDto>> ListAsync(string tenantId, string? orderId, string? status, CancellationToken ct = default)
        => (await repo.ListAsync(tenantId, Clean(orderId), Clean(status)?.ToLowerInvariant(), ct)).Select(Map).ToList();

    public async Task<Result<EInvoiceDocumentDto>> GetAsync(string tenantId, string id, CancellationToken ct = default)
    {
        var row = await repo.GetAsync(tenantId, id, ct);
        return row is null ? Error.NotFound("einvoice.doc", "Belge bulunamadı.") : Map(row);
    }

    public async Task<Result<EInvoiceDocumentDto>> RefreshStatusAsync(string tenantId, string id, CancellationToken ct = default)
    {
        var row = await repo.GetAsync(tenantId, id, ct);
        if (row is null) return Error.NotFound("einvoice.doc", "Belge bulunamadı.");
        if (string.IsNullOrWhiteSpace(row.ExternalId)) return Error.Validation("einvoice.doc.external", "Belgenin entegratör kimliği yok.");

        var status = await provider.GetStatusAsync(row.ExternalId!, ct);
        await repo.UpdateOutcomeAsync(tenantId, id, status.Success ? status.Status : "error",
            row.ExternalId, row.Uuid, row.InvoiceNumber, row.PdfUrl, status.RawResponseJson, status.Error, markIssued: false, ct);
        return await GetAsync(tenantId, id, ct);
    }

    public async Task<Result<EInvoiceDocumentDto>> CancelAsync(string tenantId, string id, string reason, CancellationToken ct = default)
    {
        var row = await repo.GetAsync(tenantId, id, ct);
        if (row is null) return Error.NotFound("einvoice.doc", "Belge bulunamadı.");
        if (string.IsNullOrWhiteSpace(row.ExternalId)) return Error.Validation("einvoice.doc.external", "Belgenin entegratör kimliği yok.");
        if (row.Status is "cancelled") return Map(row);

        var res = await provider.CancelAsync(row.ExternalId!, reason, ct);
        await repo.UpdateOutcomeAsync(tenantId, id, res.Success ? res.Status : "error",
            row.ExternalId, row.Uuid, row.InvoiceNumber, row.PdfUrl, res.RawResponseJson, res.Error, markIssued: false, ct);
        return await GetAsync(tenantId, id, ct);
    }

    private static EInvoiceDocumentDto Map(EInvoiceDocumentRow x) => new(
        x.Id, x.BranchId, x.OrderId, x.InvoiceId, x.DocumentType, x.Provider, x.Scenario, x.Status,
        x.ExternalId, x.Uuid, x.InvoiceNumber, x.BuyerName, x.BuyerTaxNo, x.Currency,
        x.Subtotal, x.TaxTotal, x.GrandTotal, x.PdfUrl, x.ErrorMessage, x.IssuedAt, x.CreatedAt);

    private static string? Clean(string? v) => string.IsNullOrWhiteSpace(v) ? null : v.Trim();
}
