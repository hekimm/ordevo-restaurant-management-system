using Dapper;
using Ordevo.BuildingBlocks.Data;
using Ordevo.Modules.Fiscal.Application;

namespace Ordevo.Modules.Fiscal.Infrastructure;

public sealed class FiscalTransactionRepository(IDbConnectionFactory factory) : IFiscalTransactionRepository
{
    public async Task<string> CreateAsync(FiscalTransactionDraft draft, CancellationToken ct = default)
    {
        var id = Guid.NewGuid().ToString();
        await using var db = await factory.CreateOpenConnectionAsync(ct);
        await db.ExecuteAsync(
            """
            INSERT INTO FISCAL_TRANSACTIONS
                (ID, TENANT_ID, BRANCH_ID, ORDER_ID, TERMINAL_ID, PROVIDER, METHOD,
                 AMOUNT, TIP_AMOUNT, CURRENCY, STATUS, REQUEST_PAYLOAD, CREATED_BY)
            VALUES
                (:id, :tenantId, :branchId, :orderId, :terminalId, :provider, :method,
                 :amount, :tipAmount, :currency, :status, :requestPayload, :createdBy)
            """,
            new OracleParams(new
            {
                id,
                draft.TenantId,
                draft.BranchId,
                draft.OrderId,
                draft.TerminalId,
                draft.Provider,
                draft.Method,
                draft.Amount,
                tipAmount = draft.Tip,
                draft.Currency,
                draft.Status,
                requestPayload = draft.RequestPayload,
                draft.CreatedBy
            }));
        return id;
    }

    public async Task AttachCommandAsync(string tenantId, string id, string commandId, string status, CancellationToken ct = default)
    {
        await using var db = await factory.CreateOpenConnectionAsync(ct);
        await db.ExecuteAsync(
            """
            UPDATE FISCAL_TRANSACTIONS
               SET COMMAND_ID = :commandId,
                   STATUS = :status,
                   UPDATED_AT = SYSTIMESTAMP,
                   ROW_VERSION = ROW_VERSION + 1
             WHERE TENANT_ID = :tenantId AND ID = :id
            """,
            new OracleParams(new { tenantId, id, commandId, status }));
    }

    public async Task CompleteAsync(
        string tenantId,
        string id,
        string? paymentId,
        PaymentTerminalResult terminalResult,
        string? documentUuid,
        string status,
        CancellationToken ct = default)
    {
        await using var db = await factory.CreateOpenConnectionAsync(ct);
        await db.ExecuteAsync(
            """
            UPDATE FISCAL_TRANSACTIONS
               SET PAYMENT_ID = :paymentId,
                   STATUS = :status,
                   AUTHORIZATION_CODE = :authorizationCode,
                   BATCH_NO = :batchNo,
                   STAN = :stan,
                   RRN = :rrn,
                   FISCAL_RECEIPT_NO = :fiscalReceiptNo,
                   Z_NO = :zNo,
                   DEVICE_SERIAL = :deviceSerial,
                   DOCUMENT_UUID = :documentUuid,
                   RESPONSE_PAYLOAD = :responsePayload,
                   ERROR_CODE = NULL,
                   ERROR_MESSAGE = NULL,
                   COMPLETED_AT = SYSTIMESTAMP,
                   UPDATED_AT = SYSTIMESTAMP,
                   ROW_VERSION = ROW_VERSION + 1
             WHERE TENANT_ID = :tenantId AND ID = :id
            """,
            new OracleParams(new
            {
                tenantId,
                id,
                paymentId,
                terminalResult.AuthorizationCode,
                terminalResult.BatchNo,
                terminalResult.Stan,
                terminalResult.Rrn,
                terminalResult.FiscalReceiptNo,
                terminalResult.ZNo,
                terminalResult.DeviceSerial,
                documentUuid,
                status,
                responsePayload = terminalResult.RawResponseJson
            }));
    }

    public async Task CompleteManualAsync(
        string tenantId,
        string id,
        string paymentId,
        string reference,
        string status,
        CancellationToken ct = default)
    {
        await using var db = await factory.CreateOpenConnectionAsync(ct);
        await db.ExecuteAsync(
            """
            UPDATE FISCAL_TRANSACTIONS
               SET PAYMENT_ID = :paymentId,
                   STATUS = CASE WHEN STATUS = 'manual_override' THEN 'manual_override' ELSE :status END,
                   AUTHORIZATION_CODE = :reference,
                   RESPONSE_PAYLOAD = JSON_OBJECT('reference' VALUE :reference RETURNING CLOB),
                   COMPLETED_AT = SYSTIMESTAMP,
                   UPDATED_AT = SYSTIMESTAMP,
                   ROW_VERSION = ROW_VERSION + 1
             WHERE TENANT_ID = :tenantId AND ID = :id
            """,
            new OracleParams(new { tenantId, id, paymentId, reference, status }));
    }

    public async Task FailAsync(
        string tenantId,
        string id,
        string? code,
        string userMessage,
        string? responsePayload,
        CancellationToken ct = default)
    {
        await using var db = await factory.CreateOpenConnectionAsync(ct);
        await db.ExecuteAsync(
            """
            UPDATE FISCAL_TRANSACTIONS
               SET STATUS = 'failed',
                   ERROR_CODE = :code,
                   ERROR_MESSAGE = :userMessage,
                   RESPONSE_PAYLOAD = :responsePayload,
                   UPDATED_AT = SYSTIMESTAMP,
                   ROW_VERSION = ROW_VERSION + 1
             WHERE TENANT_ID = :tenantId AND ID = :id
            """,
            new OracleParams(new { tenantId, id, code, userMessage, responsePayload }));
    }

    public async Task<IReadOnlyList<FiscalTransactionRow>> ListAsync(
        string tenantId,
        string? branchId,
        string? status,
        int take,
        CancellationToken ct = default)
    {
        await using var db = await factory.CreateOpenConnectionAsync(ct);
        var rows = await db.QueryAsync<FiscalTransactionRow>(
            $"""
            SELECT {SelectCols}
              FROM FISCAL_TRANSACTIONS
             WHERE TENANT_ID = :tenantId
               AND (:branchId IS NULL OR BRANCH_ID = :branchId)
               AND (:status IS NULL OR STATUS = :status)
             ORDER BY CREATED_AT DESC
             FETCH FIRST :take ROWS ONLY
            """,
            new OracleParams(new { tenantId, branchId, status, take }));
        return rows.AsList();
    }

    public async Task<FiscalTransactionRow?> GetAsync(string tenantId, string id, CancellationToken ct = default)
    {
        await using var db = await factory.CreateOpenConnectionAsync(ct);
        return await db.QueryFirstOrDefaultAsync<FiscalTransactionRow>(
            $"SELECT {SelectCols} FROM FISCAL_TRANSACTIONS WHERE TENANT_ID = :tenantId AND ID = :id",
            new OracleParams(new { tenantId, id }));
    }

    private const string SelectCols =
        """
        ID, BRANCH_ID AS BranchId, ORDER_ID AS OrderId, PAYMENT_ID AS PaymentId,
        COMMAND_ID AS CommandId, TERMINAL_ID AS TerminalId, PROVIDER, METHOD,
        AMOUNT, TIP_AMOUNT AS TipAmount, CURRENCY, STATUS,
        AUTHORIZATION_CODE AS AuthorizationCode, BATCH_NO AS BatchNo, STAN,
        RRN, FISCAL_RECEIPT_NO AS FiscalReceiptNo, Z_NO AS ZNo,
        DEVICE_SERIAL AS DeviceSerial, DOCUMENT_UUID AS DocumentUuid,
        ERROR_CODE AS ErrorCode, ERROR_MESSAGE AS ErrorMessage,
        CREATED_AT AS CreatedAt, COMPLETED_AT AS CompletedAt
        """;
}
