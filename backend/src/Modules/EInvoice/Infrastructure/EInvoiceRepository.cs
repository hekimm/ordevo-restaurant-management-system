using Dapper;
using Ordevo.BuildingBlocks.Data;
using Ordevo.Modules.EInvoice.Application;

namespace Ordevo.Modules.EInvoice.Infrastructure;

public interface IEInvoiceRepository
{
    Task<OrderInvoiceHeaderRow?> GetOrderHeaderAsync(string tenantId, string orderId, CancellationToken ct = default);
    Task<IReadOnlyList<OrderInvoiceLineRow>> GetOrderLinesAsync(string tenantId, string orderId, CancellationToken ct = default);
    Task<string> InsertDraftAsync(string tenantId, string? branchId, string orderId, string documentType,
        string provider, string scenario, string currency, decimal subtotal, decimal taxTotal, decimal grandTotal,
        string? buyerName, string? buyerTaxNo, string requestPayloadJson, string? userId, CancellationToken ct = default);
    Task UpdateOutcomeAsync(string tenantId, string id, string status, string? externalId, string? uuid,
        string? invoiceNumber, string? pdfUrl, string? responseJson, string? error, bool markIssued, CancellationToken ct = default);
    Task<IReadOnlyList<EInvoiceDocumentRow>> ListAsync(string tenantId, string? orderId, string? status, CancellationToken ct = default);
    Task<EInvoiceDocumentRow?> GetAsync(string tenantId, string id, CancellationToken ct = default);
}

public sealed class EInvoiceRepository(IDbConnectionFactory factory) : IEInvoiceRepository
{
    public async Task<OrderInvoiceHeaderRow?> GetOrderHeaderAsync(string tenantId, string orderId, CancellationToken ct = default)
    {
        await using var db = await factory.CreateOpenConnectionAsync(ct);
        var row = await db.QueryFirstOrDefaultAsync<OrderInvoiceHeaderRow>(
            """
            SELECT o.ID AS OrderId, o.ORDER_NO AS OrderNo, o.BRANCH_ID AS BranchId, o.STATUS,
                   o.SUBTOTAL, o.DISCOUNT_TOTAL AS DiscountTotal, o.TAX_TOTAL AS TaxTotal, o.TOTAL,
                   o.OPENED_AT AS OpenedAt, o.CLOSED_AT AS ClosedAt,
                   t.NAME AS SellerName, b.NAME AS BranchName,
                   NVL(b.CURRENCY, 'TRY') AS Currency
            FROM ORDERS o
            JOIN TENANTS t  ON t.ID = o.TENANT_ID
            LEFT JOIN BRANCHES b ON b.ID = o.BRANCH_ID
            WHERE o.TENANT_ID = :tenantId AND o.ID = :orderId
            """,
            new OracleParams(new { tenantId, orderId }));
        return row;
    }

    public async Task<IReadOnlyList<OrderInvoiceLineRow>> GetOrderLinesAsync(string tenantId, string orderId, CancellationToken ct = default)
    {
        await using var db = await factory.CreateOpenConnectionAsync(ct);
        var rows = await db.QueryAsync<OrderInvoiceLineRow>(
            """
            SELECT NAME_SNAPSHOT AS Name, QUANTITY, UNIT_PRICE AS UnitPrice,
                   MODIFIER_TOTAL AS ModifierTotal, LINE_TOTAL AS LineTotal, VAT_RATE AS VatRate
            FROM ORDER_ITEMS
            WHERE TENANT_ID = :tenantId AND ORDER_ID = :orderId
              AND STATUS <> 'void' AND IS_COMP = 0
            ORDER BY COURSE_NO, CREATED_AT
            """,
            new OracleParams(new { tenantId, orderId }));
        return rows.AsList();
    }

    public async Task<string> InsertDraftAsync(string tenantId, string? branchId, string orderId, string documentType,
        string provider, string scenario, string currency, decimal subtotal, decimal taxTotal, decimal grandTotal,
        string? buyerName, string? buyerTaxNo, string requestPayloadJson, string? userId, CancellationToken ct = default)
    {
        var id = Guid.NewGuid().ToString();
        await using var db = await factory.CreateOpenConnectionAsync(ct);
        await db.ExecuteAsync(
            """
            INSERT INTO EINVOICE_DOCUMENTS
                (ID, TENANT_ID, BRANCH_ID, ORDER_ID, DOCUMENT_TYPE, PROVIDER, SCENARIO, STATUS,
                 BUYER_NAME, BUYER_TAX_NO, CURRENCY, SUBTOTAL, TAX_TOTAL, GRAND_TOTAL, REQUEST_PAYLOAD, CREATED_BY)
            VALUES
                (:id, :tenantId, :branchId, :orderId, :documentType, :provider, :scenario, 'draft',
                 :buyerName, :buyerTaxNo, :currency, :subtotal, :taxTotal, :grandTotal, :requestPayloadJson, :userId)
            """,
            new OracleParams(new
            {
                id, tenantId, branchId, orderId, documentType, provider, scenario,
                buyerName, buyerTaxNo, currency, subtotal, taxTotal, grandTotal, requestPayloadJson, userId
            }));
        return id;
    }

    public async Task UpdateOutcomeAsync(string tenantId, string id, string status, string? externalId, string? uuid,
        string? invoiceNumber, string? pdfUrl, string? responseJson, string? error, bool markIssued, CancellationToken ct = default)
    {
        await using var db = await factory.CreateOpenConnectionAsync(ct);
        await db.ExecuteAsync(
            $"""
            UPDATE EINVOICE_DOCUMENTS
               SET STATUS = :status,
                   EXTERNAL_ID = :externalId,
                   DOC_UUID = :uuid,
                   INVOICE_NUMBER = :invoiceNumber,
                   PDF_URL = :pdfUrl,
                   RESPONSE_PAYLOAD = :responseJson,
                   ERROR_MESSAGE = :error,
                   {(markIssued ? "ISSUED_AT = SYSTIMESTAMP," : "")}
                   UPDATED_AT = SYSTIMESTAMP,
                   ROW_VERSION = ROW_VERSION + 1
             WHERE TENANT_ID = :tenantId AND ID = :id
            """,
            new OracleParams(new { tenantId, id, status, externalId, uuid, invoiceNumber, pdfUrl, responseJson, error }));
    }

    public async Task<IReadOnlyList<EInvoiceDocumentRow>> ListAsync(string tenantId, string? orderId, string? status, CancellationToken ct = default)
    {
        await using var db = await factory.CreateOpenConnectionAsync(ct);
        var rows = await db.QueryAsync<EInvoiceDocumentRow>(
            $"""
            SELECT {SelectCols}
            FROM EINVOICE_DOCUMENTS
            WHERE TENANT_ID = :tenantId
              AND (:orderId IS NULL OR ORDER_ID = :orderId)
              AND (:status  IS NULL OR STATUS   = :status)
            ORDER BY CREATED_AT DESC
            """,
            new OracleParams(new { tenantId, orderId, status }));
        return rows.AsList();
    }

    public async Task<EInvoiceDocumentRow?> GetAsync(string tenantId, string id, CancellationToken ct = default)
    {
        await using var db = await factory.CreateOpenConnectionAsync(ct);
        return await db.QueryFirstOrDefaultAsync<EInvoiceDocumentRow>(
            $"SELECT {SelectCols} FROM EINVOICE_DOCUMENTS WHERE TENANT_ID = :tenantId AND ID = :id",
            new OracleParams(new { tenantId, id }));
    }

    private const string SelectCols =
        """
        ID, BRANCH_ID AS BranchId, ORDER_ID AS OrderId, INVOICE_ID AS InvoiceId,
        DOCUMENT_TYPE AS DocumentType, PROVIDER, SCENARIO, STATUS, EXTERNAL_ID AS ExternalId,
        DOC_UUID AS Uuid, INVOICE_NUMBER AS InvoiceNumber, BUYER_NAME AS BuyerName,
        BUYER_TAX_NO AS BuyerTaxNo, CURRENCY, SUBTOTAL, TAX_TOTAL AS TaxTotal, GRAND_TOTAL AS GrandTotal,
        PDF_URL AS PdfUrl, ERROR_MESSAGE AS ErrorMessage, ISSUED_AT AS IssuedAt, CREATED_AT AS CreatedAt
        """;
}
