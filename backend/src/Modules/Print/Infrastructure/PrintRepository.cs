using System.Text.Json;
using Dapper;
using Ordevo.BuildingBlocks.Data;
using Ordevo.Modules.Print.Application;

namespace Ordevo.Modules.Print.Infrastructure;

public interface IPrintRepository
{
    Task<ReceiptHeaderRow?> GetReceiptHeaderAsync(string tenantId, string branchId, string orderId, CancellationToken ct = default);
    Task<IReadOnlyList<ReceiptLineRow>> GetReceiptLinesAsync(string orderId, CancellationToken ct = default);
    Task<IReadOnlyList<ReceiptPaymentRow>> GetReceiptPaymentsAsync(string tenantId, string orderId, CancellationToken ct = default);
    Task<IReadOnlyList<KitchenTicketLineRow>> GetKitchenLinesAsync(string orderId, CancellationToken ct = default);
    Task<PrintJobRow> QueueAsync(string tenantId, string branchId, string userId, string jobType, string orderId, object payload, string? terminalId, int copies, string? printerName, CancellationToken ct = default);
    Task<IReadOnlyList<PrintJobRow>> ListJobsAsync(string tenantId, string branchId, string? status, int take, CancellationToken ct = default);
}

public sealed class PrintRepository(IDbConnectionFactory factory) : IPrintRepository
{
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web);

    public async Task<ReceiptHeaderRow?> GetReceiptHeaderAsync(string tenantId, string branchId, string orderId, CancellationToken ct = default)
    {
        await using var db = await factory.CreateOpenConnectionAsync(ct);
        return await db.QuerySingleOrDefaultAsync<ReceiptHeaderRow>(
            """
            SELECT o.ID AS OrderId, o.ORDER_NO AS OrderNo, dt.NAME AS TableName, o.ORDER_TYPE AS OrderType,
                   o.STATUS, i.INVOICE_NO AS InvoiceNo, o.SUBTOTAL, o.DISCOUNT_TOTAL AS DiscountTotal,
                   o.TAX_TOTAL AS TaxTotal, o.TOTAL, o.OPENED_AT AS OpenedAt, o.CLOSED_AT AS ClosedAt
            FROM ORDERS o
            LEFT JOIN DINING_TABLES dt ON dt.ID = o.TABLE_ID
            LEFT JOIN INVOICES i ON i.ORDER_ID = o.ID
            WHERE o.TENANT_ID = :tenantId AND o.BRANCH_ID = :branchId AND o.ID = :orderId
            """,
            new OracleParams(new { tenantId, branchId, orderId }));
    }

    public async Task<IReadOnlyList<ReceiptLineRow>> GetReceiptLinesAsync(string orderId, CancellationToken ct = default)
    {
        await using var db = await factory.CreateOpenConnectionAsync(ct);
        var rows = await db.QueryAsync<ReceiptLineRow>(
            """
            SELECT NAME_SNAPSHOT AS Name, QUANTITY, UNIT_PRICE AS UnitPrice, LINE_TOTAL AS LineTotal, NOTE
            FROM ORDER_ITEMS
            WHERE ORDER_ID = :orderId AND STATUS <> 'void'
            ORDER BY CREATED_AT, ID
            """,
            new OracleParams(new { orderId }));
        return rows.AsList();
    }

    public async Task<IReadOnlyList<ReceiptPaymentRow>> GetReceiptPaymentsAsync(string tenantId, string orderId, CancellationToken ct = default)
    {
        await using var db = await factory.CreateOpenConnectionAsync(ct);
        var rows = await db.QueryAsync<ReceiptPaymentRow>(
            """
            SELECT METHOD, AMOUNT, TIP_AMOUNT AS TipAmount, REFERENCE
            FROM PAYMENTS
            WHERE TENANT_ID = :tenantId AND ORDER_ID = :orderId AND IS_VOIDED = 0
            ORDER BY CREATED_AT, ID
            """,
            new OracleParams(new { tenantId, orderId }));
        return rows.AsList();
    }

    public async Task<IReadOnlyList<KitchenTicketLineRow>> GetKitchenLinesAsync(string orderId, CancellationToken ct = default)
    {
        await using var db = await factory.CreateOpenConnectionAsync(ct);
        var rows = await db.QueryAsync<KitchenTicketLineRow>(
            """
            SELECT oi.NAME_SNAPSHOT AS Name, oi.QUANTITY, oi.COURSE_NO AS CourseNo, oi.STATUS,
                   mi.PREP_STATION AS Station, oi.NOTE,
                   LISTAGG(oim.NAME_SNAPSHOT, ', ') WITHIN GROUP (ORDER BY oim.NAME_SNAPSHOT) AS Modifiers
            FROM ORDER_ITEMS oi
            LEFT JOIN MENU_ITEMS mi ON mi.ID = oi.MENU_ITEM_ID
            LEFT JOIN ORDER_ITEM_MODIFIERS oim ON oim.ORDER_ITEM_ID = oi.ID
            WHERE oi.ORDER_ID = :orderId AND oi.STATUS <> 'void'
            GROUP BY oi.ID, oi.NAME_SNAPSHOT, oi.QUANTITY, oi.COURSE_NO, oi.STATUS, mi.PREP_STATION, oi.NOTE, oi.CREATED_AT
            ORDER BY oi.COURSE_NO, mi.PREP_STATION, oi.CREATED_AT
            """,
            new OracleParams(new { orderId }));
        return rows.AsList();
    }

    public async Task<PrintJobRow> QueueAsync(string tenantId, string branchId, string userId, string jobType, string orderId, object payload, string? terminalId, int copies, string? printerName, CancellationToken ct = default)
    {
        var id = Guid.NewGuid().ToString();
        var json = JsonSerializer.Serialize(new
        {
            jobId = id,
            jobType,
            orderId,
            printerName,
            copies,
            document = payload
        }, JsonOptions);

        await using var db = await factory.CreateOpenConnectionAsync(ct);
        await db.ExecuteAsync(
            """
            INSERT INTO PRINT_JOBS (ID, TENANT_ID, BRANCH_ID, JOB_TYPE, ORDER_ID, TERMINAL_ID, STATUS, COPIES, PAYLOAD, CREATED_BY)
            VALUES (:id, :tenantId, :branchId, :jobType, :orderId, :terminalId, 'queued', :copies, :payload, :userId)
            """,
            new OracleParams(new { id, tenantId, branchId, jobType, orderId, terminalId, copies, payload = json, userId }));

        if (!string.IsNullOrWhiteSpace(terminalId))
        {
            await db.ExecuteAsync(
                """
                INSERT INTO INTEGRATION_COMMANDS (
                  ID, TENANT_ID, BRANCH_ID, TERMINAL_ID, ORDER_ID, COMMAND_TYPE, IDEMPOTENCY_KEY, PAYLOAD, STATUS, REQUESTED_BY
                )
                VALUES (
                  :commandId, :tenantId, :branchId, :terminalId, :orderId, 'print', :idempotencyKey, :payload, 'queued', :userId
                )
                """,
                new OracleParams(new
                {
                    commandId = Guid.NewGuid().ToString(),
                    tenantId,
                    branchId,
                    terminalId,
                    orderId,
                    idempotencyKey = $"print:{id}",
                    payload = json,
                    userId
                }));
        }

        return await GetJobAsync(db, tenantId, id);
    }

    public async Task<IReadOnlyList<PrintJobRow>> ListJobsAsync(string tenantId, string branchId, string? status, int take, CancellationToken ct = default)
    {
        await using var db = await factory.CreateOpenConnectionAsync(ct);
        var rows = await db.QueryAsync<PrintJobRow>(
            """
            SELECT ID, BRANCH_ID AS BranchId, JOB_TYPE AS JobType, ORDER_ID AS OrderId, TERMINAL_ID AS TerminalId,
                   STATUS, COPIES, ERROR_MESSAGE AS ErrorMessage, CREATED_AT AS CreatedAt, UPDATED_AT AS UpdatedAt
            FROM PRINT_JOBS
            WHERE TENANT_ID = :tenantId AND BRANCH_ID = :branchId AND (:status IS NULL OR STATUS = :status)
            ORDER BY CREATED_AT DESC
            FETCH FIRST :take ROWS ONLY
            """,
            new OracleParams(new { tenantId, branchId, status, take }));
        return rows.AsList();
    }

    private static Task<PrintJobRow> GetJobAsync(System.Data.IDbConnection db, string tenantId, string id)
        => db.QuerySingleAsync<PrintJobRow>(
            """
            SELECT ID, BRANCH_ID AS BranchId, JOB_TYPE AS JobType, ORDER_ID AS OrderId, TERMINAL_ID AS TerminalId,
                   STATUS, COPIES, ERROR_MESSAGE AS ErrorMessage, CREATED_AT AS CreatedAt, UPDATED_AT AS UpdatedAt
            FROM PRINT_JOBS
            WHERE TENANT_ID = :tenantId AND ID = :id
            """,
            new OracleParams(new { tenantId, id }));
}
