using Dapper;
using Ordevo.BuildingBlocks.Data;
using Ordevo.Modules.Finance.Application;

namespace Ordevo.Modules.Finance.Infrastructure;

public interface IFinanceRepository
{
    Task<IReadOnlyList<FinanceAccountRow>> ListAccountsAsync(string tenantId, string? branchId, CancellationToken ct = default);
    Task<FinanceAccountRow> CreateAccountAsync(string tenantId, string? branchId, CreateFinanceAccountRequest request, CancellationToken ct = default);
    Task<FinanceAccountRow?> UpdateAccountAsync(string tenantId, string id, UpdateFinanceAccountRequest request, CancellationToken ct = default);
    Task<int> DeactivateAccountAsync(string tenantId, string id, CancellationToken ct = default);
    Task<IReadOnlyList<CounterpartyRow>> ListCounterpartiesAsync(string tenantId, string? type, CancellationToken ct = default);
    Task<CounterpartyRow> CreateCounterpartyAsync(string tenantId, CreateCounterpartyRequest request, CancellationToken ct = default);
    Task<CounterpartyRow?> UpdateCounterpartyAsync(string tenantId, string id, UpdateCounterpartyRequest request, CancellationToken ct = default);
    Task<int> DeactivateCounterpartyAsync(string tenantId, string id, CancellationToken ct = default);
    Task<IReadOnlyList<FinanceTransactionRow>> ListTransactionsAsync(string tenantId, string branchId, DateTime start, DateTime end, string? type, CancellationToken ct = default);
    Task<FinanceTransactionRow> CreateTransactionAsync(string tenantId, string branchId, string userId, CreateFinanceTransactionRequest request, CancellationToken ct = default);
    Task<FinanceTransactionRow?> UpdateTransactionAsync(string tenantId, string branchId, string id, CreateFinanceTransactionRequest request, CancellationToken ct = default);
    Task<int> VoidTransactionAsync(string tenantId, string branchId, string id, CancellationToken ct = default);
    Task<FinanceSummaryRow> SummaryAsync(string tenantId, string branchId, DateTime start, DateTime end, CancellationToken ct = default);
    Task<IReadOnlyList<CashflowDayRow>> CashflowAsync(string tenantId, string branchId, DateTime start, DateTime end, CancellationToken ct = default);
}

public sealed class FinanceRepository(IDbConnectionFactory factory) : IFinanceRepository
{
    public async Task<IReadOnlyList<FinanceAccountRow>> ListAccountsAsync(string tenantId, string? branchId, CancellationToken ct = default)
    {
        await using var db = await factory.CreateOpenConnectionAsync(ct);
        var rows = await db.QueryAsync<FinanceAccountRow>(
            """
            SELECT ID, BRANCH_ID AS BranchId, NAME, ACCOUNT_TYPE AS AccountType, CURRENCY,
                   OPENING_BALANCE AS OpeningBalance, IS_ACTIVE AS IsActive, CREATED_AT AS CreatedAt
            FROM FIN_ACCOUNTS
            WHERE TENANT_ID = :tenantId
              AND (:branchId IS NULL OR BRANCH_ID IS NULL OR BRANCH_ID = :branchId)
            ORDER BY IS_ACTIVE DESC, NAME
            """,
            new OracleParams(new { tenantId, branchId }));
        return rows.AsList();
    }

    public async Task<FinanceAccountRow> CreateAccountAsync(string tenantId, string? branchId, CreateFinanceAccountRequest request, CancellationToken ct = default)
    {
        var id = Guid.NewGuid().ToString();
        await using var db = await factory.CreateOpenConnectionAsync(ct);
        await db.ExecuteAsync(
            """
            INSERT INTO FIN_ACCOUNTS (ID, TENANT_ID, BRANCH_ID, NAME, ACCOUNT_TYPE, CURRENCY, OPENING_BALANCE)
            VALUES (:id, :tenantId, :branchId, :name, :accountType, :currency, :openingBalance)
            """,
            new OracleParams(new
            {
                id,
                tenantId,
                branchId,
                name = request.Name,
                accountType = request.AccountType,
                currency = request.Currency ?? "TRY",
                openingBalance = request.OpeningBalance
            }));

        return await GetAccountAsync(db, tenantId, id);
    }

    public async Task<FinanceAccountRow?> UpdateAccountAsync(string tenantId, string id, UpdateFinanceAccountRequest request, CancellationToken ct = default)
    {
        await using var db = await factory.CreateOpenConnectionAsync(ct);
        var rows = await db.ExecuteAsync(
            """
            UPDATE FIN_ACCOUNTS
               SET NAME = :name, ACCOUNT_TYPE = :accountType, CURRENCY = :currency,
                   OPENING_BALANCE = :openingBalance, IS_ACTIVE = :isActive,
                   UPDATED_AT = SYSTIMESTAMP, ROW_VERSION = ROW_VERSION + 1
             WHERE TENANT_ID = :tenantId AND ID = :id
            """,
            new OracleParams(new
            {
                tenantId,
                id,
                name = request.Name,
                accountType = request.AccountType,
                currency = request.Currency ?? "TRY",
                openingBalance = request.OpeningBalance,
                isActive = request.IsActive
            }));

        return rows == 0 ? null : await GetAccountAsync(db, tenantId, id);
    }

    public async Task<int> DeactivateAccountAsync(string tenantId, string id, CancellationToken ct = default)
    {
        await using var db = await factory.CreateOpenConnectionAsync(ct);
        return await db.ExecuteAsync(
            "UPDATE FIN_ACCOUNTS SET IS_ACTIVE = 0, UPDATED_AT = SYSTIMESTAMP, ROW_VERSION = ROW_VERSION + 1 WHERE TENANT_ID = :tenantId AND ID = :id",
            new OracleParams(new { tenantId, id }));
    }

    public async Task<IReadOnlyList<CounterpartyRow>> ListCounterpartiesAsync(string tenantId, string? type, CancellationToken ct = default)
    {
        await using var db = await factory.CreateOpenConnectionAsync(ct);
        var rows = await db.QueryAsync<CounterpartyRow>(
            """
            SELECT ID, COUNTERPARTY_TYPE AS CounterpartyType, REF_ID AS RefId, NAME, PHONE, EMAIL, TAX_NO AS TaxNo,
                   IS_ACTIVE AS IsActive, CREATED_AT AS CreatedAt
            FROM FIN_COUNTERPARTIES
            WHERE TENANT_ID = :tenantId AND (:type IS NULL OR COUNTERPARTY_TYPE = :type)
            ORDER BY IS_ACTIVE DESC, NAME
            """,
            new OracleParams(new { tenantId, type }));
        return rows.AsList();
    }

    public async Task<CounterpartyRow> CreateCounterpartyAsync(string tenantId, CreateCounterpartyRequest request, CancellationToken ct = default)
    {
        var id = Guid.NewGuid().ToString();
        await using var db = await factory.CreateOpenConnectionAsync(ct);
        await db.ExecuteAsync(
            """
            INSERT INTO FIN_COUNTERPARTIES (ID, TENANT_ID, COUNTERPARTY_TYPE, REF_ID, NAME, PHONE, EMAIL, TAX_NO)
            VALUES (:id, :tenantId, :type, :refId, :name, :phone, :email, :taxNo)
            """,
            new OracleParams(new
            {
                id,
                tenantId,
                type = request.CounterpartyType,
                refId = request.RefId,
                name = request.Name,
                phone = request.Phone,
                email = request.Email,
                taxNo = request.TaxNo
            }));

        return await db.QuerySingleAsync<CounterpartyRow>(
            """
            SELECT ID, COUNTERPARTY_TYPE AS CounterpartyType, REF_ID AS RefId, NAME, PHONE, EMAIL, TAX_NO AS TaxNo,
                   IS_ACTIVE AS IsActive, CREATED_AT AS CreatedAt
            FROM FIN_COUNTERPARTIES
            WHERE TENANT_ID = :tenantId AND ID = :id
            """,
            new OracleParams(new { tenantId, id }));
    }

    public async Task<CounterpartyRow?> UpdateCounterpartyAsync(string tenantId, string id, UpdateCounterpartyRequest request, CancellationToken ct = default)
    {
        await using var db = await factory.CreateOpenConnectionAsync(ct);
        var rows = await db.ExecuteAsync(
            """
            UPDATE FIN_COUNTERPARTIES
               SET COUNTERPARTY_TYPE = :type, REF_ID = :refId, NAME = :name,
                   PHONE = :phone, EMAIL = :email, TAX_NO = :taxNo, IS_ACTIVE = :isActive,
                   UPDATED_AT = SYSTIMESTAMP, ROW_VERSION = ROW_VERSION + 1
             WHERE TENANT_ID = :tenantId AND ID = :id
            """,
            new OracleParams(new
            {
                tenantId,
                id,
                type = request.CounterpartyType,
                refId = request.RefId,
                name = request.Name,
                phone = request.Phone,
                email = request.Email,
                taxNo = request.TaxNo,
                isActive = request.IsActive
            }));

        return rows == 0 ? null : await db.QuerySingleAsync<CounterpartyRow>(
            """
            SELECT ID, COUNTERPARTY_TYPE AS CounterpartyType, REF_ID AS RefId, NAME, PHONE, EMAIL, TAX_NO AS TaxNo,
                   IS_ACTIVE AS IsActive, CREATED_AT AS CreatedAt
            FROM FIN_COUNTERPARTIES
            WHERE TENANT_ID = :tenantId AND ID = :id
            """,
            new OracleParams(new { tenantId, id }));
    }

    public async Task<int> DeactivateCounterpartyAsync(string tenantId, string id, CancellationToken ct = default)
    {
        await using var db = await factory.CreateOpenConnectionAsync(ct);
        return await db.ExecuteAsync(
            "UPDATE FIN_COUNTERPARTIES SET IS_ACTIVE = 0, UPDATED_AT = SYSTIMESTAMP, ROW_VERSION = ROW_VERSION + 1 WHERE TENANT_ID = :tenantId AND ID = :id",
            new OracleParams(new { tenantId, id }));
    }

    public async Task<IReadOnlyList<FinanceTransactionRow>> ListTransactionsAsync(string tenantId, string branchId, DateTime start, DateTime end, string? type, CancellationToken ct = default)
    {
        await using var db = await factory.CreateOpenConnectionAsync(ct);
        var rows = await db.QueryAsync<FinanceTransactionRow>(
            """
            SELECT ID, BRANCH_ID AS BranchId, ACCOUNT_ID AS AccountId, COUNTERPARTY_ID AS CounterpartyId,
                   TXN_TYPE AS TransactionType, CATEGORY, METHOD, AMOUNT, TAX_AMOUNT AS TaxAmount,
                   BUSINESS_DATE AS BusinessDate, DESCRIPTION, SOURCE_MODULE AS SourceModule, SOURCE_ID AS SourceId,
                   IS_VOIDED AS IsVoided, CREATED_AT AS CreatedAt
            FROM FIN_TRANSACTIONS
            WHERE TENANT_ID = :tenantId AND BRANCH_ID = :branchId AND BUSINESS_DATE BETWEEN :startDate AND :endDate
              AND (:type IS NULL OR TXN_TYPE = :type)
            ORDER BY BUSINESS_DATE DESC, CREATED_AT DESC
            FETCH FIRST 200 ROWS ONLY
            """,
            new OracleParams(new { tenantId, branchId, startDate = start, endDate = end, type }));
        return rows.AsList();
    }

    public async Task<FinanceTransactionRow> CreateTransactionAsync(string tenantId, string branchId, string userId, CreateFinanceTransactionRequest request, CancellationToken ct = default)
    {
        var id = Guid.NewGuid().ToString();
        await using var db = await factory.CreateOpenConnectionAsync(ct);
        await db.ExecuteAsync(
            """
            INSERT INTO FIN_TRANSACTIONS (
              ID, TENANT_ID, BRANCH_ID, ACCOUNT_ID, COUNTERPARTY_ID, TXN_TYPE, CATEGORY, METHOD,
              AMOUNT, TAX_AMOUNT, BUSINESS_DATE, DESCRIPTION, SOURCE_MODULE, SOURCE_ID, CREATED_BY
            )
            VALUES (
              :id, :tenantId, :branchId, :accountId, :counterpartyId, :transactionType, :category, :method,
              :amount, :taxAmount, :businessDate, :description, 'finance', :id, :userId
            )
            """,
            new OracleParams(new
            {
                id,
                tenantId,
                branchId,
                accountId = request.AccountId,
                counterpartyId = request.CounterpartyId,
                transactionType = request.TransactionType,
                category = request.Category,
                method = request.Method,
                amount = request.Amount,
                taxAmount = request.TaxAmount,
                businessDate = request.BusinessDate ?? DateTime.UtcNow.Date,
                description = request.Description,
                userId
            }));

        return await GetTransactionAsync(db, tenantId, id);
    }

    public async Task<FinanceTransactionRow?> UpdateTransactionAsync(string tenantId, string branchId, string id, CreateFinanceTransactionRequest request, CancellationToken ct = default)
    {
        await using var db = await factory.CreateOpenConnectionAsync(ct);
        var rows = await db.ExecuteAsync(
            """
            UPDATE FIN_TRANSACTIONS
               SET ACCOUNT_ID = :accountId,
                   COUNTERPARTY_ID = :counterpartyId,
                   TXN_TYPE = :transactionType,
                   CATEGORY = :category,
                   METHOD = :method,
                   AMOUNT = :amount,
                   TAX_AMOUNT = :taxAmount,
                   BUSINESS_DATE = :businessDate,
                   DESCRIPTION = :description,
                   UPDATED_AT = SYSTIMESTAMP,
                   ROW_VERSION = ROW_VERSION + 1
             WHERE TENANT_ID = :tenantId
               AND BRANCH_ID = :branchId
               AND ID = :id
               AND SOURCE_MODULE = 'finance'
               AND IS_VOIDED = 0
            """,
            new OracleParams(new
            {
                tenantId,
                branchId,
                id,
                accountId = request.AccountId,
                counterpartyId = request.CounterpartyId,
                transactionType = request.TransactionType,
                category = request.Category,
                method = request.Method,
                amount = request.Amount,
                taxAmount = request.TaxAmount,
                businessDate = request.BusinessDate ?? DateTime.UtcNow.Date,
                description = request.Description
            }));

        return rows == 0 ? null : await GetTransactionAsync(db, tenantId, id);
    }

    public async Task<int> VoidTransactionAsync(string tenantId, string branchId, string id, CancellationToken ct = default)
    {
        await using var db = await factory.CreateOpenConnectionAsync(ct);
        return await db.ExecuteAsync(
            """
            UPDATE FIN_TRANSACTIONS
               SET IS_VOIDED = 1, UPDATED_AT = SYSTIMESTAMP, ROW_VERSION = ROW_VERSION + 1
             WHERE TENANT_ID = :tenantId AND BRANCH_ID = :branchId AND ID = :id AND SOURCE_MODULE = 'finance' AND IS_VOIDED = 0
            """,
            new OracleParams(new { tenantId, branchId, id }));
    }

    public async Task<FinanceSummaryRow> SummaryAsync(string tenantId, string branchId, DateTime start, DateTime end, CancellationToken ct = default)
    {
        await using var db = await factory.CreateOpenConnectionAsync(ct);
        return await db.QuerySingleAsync<FinanceSummaryRow>(
            """
            SELECT
              (SELECT NVL(SUM(TOTAL), 0)
                 FROM ORDERS
                WHERE TENANT_ID = :tenantId AND BRANCH_ID = :branchId AND STATUS = 'closed'
                  AND TRUNC(CLOSED_AT) BETWEEN :startDate AND :endDate) AS SalesRevenue,
              (SELECT NVL(SUM(AMOUNT), 0)
                 FROM FIN_TRANSACTIONS
                WHERE TENANT_ID = :tenantId AND BRANCH_ID = :branchId AND TXN_TYPE = 'income' AND IS_VOIDED = 0
                  AND BUSINESS_DATE BETWEEN :startDate AND :endDate) AS OtherIncome,
              (SELECT NVL(SUM(r.AMOUNT), 0)
                 FROM REFUNDS r
                 JOIN ORDERS o ON o.ID = r.ORDER_ID
                WHERE r.TENANT_ID = :tenantId AND o.BRANCH_ID = :branchId
                  AND TRUNC(r.CREATED_AT) BETWEEN :startDate AND :endDate) AS Refunds,
              (SELECT NVL(SUM(TOTAL), 0)
                 FROM PURCHASE_ORDERS
                WHERE TENANT_ID = :tenantId AND BRANCH_ID = :branchId AND STATUS = 'received'
                  AND TRUNC(NVL(RECEIVED_AT, CREATED_AT)) BETWEEN :startDate AND :endDate) AS PurchaseCosts,
              (SELECT NVL(SUM(AMOUNT), 0)
                 FROM FIN_TRANSACTIONS
                WHERE TENANT_ID = :tenantId AND BRANCH_ID = :branchId AND TXN_TYPE = 'expense' AND IS_VOIDED = 0
                  AND BUSINESS_DATE BETWEEN :startDate AND :endDate) AS Expenses,
              (SELECT NVL(SUM(CASE WHEN MOVE_TYPE IN ('sale','payin','opening') THEN AMOUNT ELSE 0 END), 0)
                 FROM CASH_MOVEMENTS
                WHERE TENANT_ID = :tenantId AND BRANCH_ID = :branchId
                  AND TRUNC(CREATED_AT) BETWEEN :startDate AND :endDate) AS CashIn,
              (SELECT ABS(NVL(SUM(CASE WHEN MOVE_TYPE IN ('refund','payout','closing') THEN AMOUNT ELSE 0 END), 0))
                 FROM CASH_MOVEMENTS
                WHERE TENANT_ID = :tenantId AND BRANCH_ID = :branchId
                  AND TRUNC(CREATED_AT) BETWEEN :startDate AND :endDate) AS CashOut,
              (SELECT NVL(SUM(AMOUNT), 0)
                 FROM PAYMENTS
                WHERE TENANT_ID = :tenantId AND BRANCH_ID = :branchId AND METHOD = 'on_account' AND IS_VOIDED = 0
                  AND TRUNC(CREATED_AT) BETWEEN :startDate AND :endDate) AS Receivables,
              (SELECT NVL(SUM(AMOUNT), 0)
                 FROM FIN_TRANSACTIONS
                WHERE TENANT_ID = :tenantId AND BRANCH_ID = :branchId AND TXN_TYPE = 'expense' AND METHOD = 'on_account' AND IS_VOIDED = 0
                  AND BUSINESS_DATE BETWEEN :startDate AND :endDate) AS Payables
            FROM DUAL
            """,
            new OracleParams(new { tenantId, branchId, startDate = start, endDate = end }));
    }

    public async Task<IReadOnlyList<CashflowDayRow>> CashflowAsync(string tenantId, string branchId, DateTime start, DateTime end, CancellationToken ct = default)
    {
        await using var db = await factory.CreateOpenConnectionAsync(ct);
        var rows = await db.QueryAsync<CashflowDayRow>(
            """
            WITH flow AS (
              SELECT TRUNC(CLOSED_AT) AS Dt, NVL(SUM(TOTAL), 0) AS Income, 0 AS Expense
                FROM ORDERS
               WHERE TENANT_ID = :tenantId AND BRANCH_ID = :branchId AND STATUS = 'closed'
                 AND TRUNC(CLOSED_AT) BETWEEN :startDate AND :endDate
               GROUP BY TRUNC(CLOSED_AT)
              UNION ALL
              SELECT BUSINESS_DATE AS Dt,
                     NVL(SUM(CASE WHEN TXN_TYPE = 'income' THEN AMOUNT ELSE 0 END), 0) AS Income,
                     NVL(SUM(CASE WHEN TXN_TYPE = 'expense' THEN AMOUNT ELSE 0 END), 0) AS Expense
                FROM FIN_TRANSACTIONS
               WHERE TENANT_ID = :tenantId AND BRANCH_ID = :branchId AND IS_VOIDED = 0
                 AND BUSINESS_DATE BETWEEN :startDate AND :endDate
               GROUP BY BUSINESS_DATE
              UNION ALL
              SELECT TRUNC(NVL(RECEIVED_AT, CREATED_AT)) AS Dt, 0 AS Income, NVL(SUM(TOTAL), 0) AS Expense
                FROM PURCHASE_ORDERS
               WHERE TENANT_ID = :tenantId AND BRANCH_ID = :branchId AND STATUS = 'received'
                 AND TRUNC(NVL(RECEIVED_AT, CREATED_AT)) BETWEEN :startDate AND :endDate
               GROUP BY TRUNC(NVL(RECEIVED_AT, CREATED_AT))
            )
            SELECT TO_CHAR(Dt, 'YYYY-MM-DD') AS BusinessDate,
                   SUM(Income) AS Income,
                   SUM(Expense) AS Expense,
                   SUM(Income) - SUM(Expense) AS Net
            FROM flow
            GROUP BY Dt
            ORDER BY Dt
            """,
            new OracleParams(new { tenantId, branchId, startDate = start, endDate = end }));
        return rows.AsList();
    }

    private static Task<FinanceAccountRow> GetAccountAsync(System.Data.IDbConnection db, string tenantId, string id)
        => db.QuerySingleAsync<FinanceAccountRow>(
            """
            SELECT ID, BRANCH_ID AS BranchId, NAME, ACCOUNT_TYPE AS AccountType, CURRENCY,
                   OPENING_BALANCE AS OpeningBalance, IS_ACTIVE AS IsActive, CREATED_AT AS CreatedAt
            FROM FIN_ACCOUNTS
            WHERE TENANT_ID = :tenantId AND ID = :id
            """,
            new OracleParams(new { tenantId, id }));

    private static Task<FinanceTransactionRow> GetTransactionAsync(System.Data.IDbConnection db, string tenantId, string id)
        => db.QuerySingleAsync<FinanceTransactionRow>(
            """
            SELECT ID, BRANCH_ID AS BranchId, ACCOUNT_ID AS AccountId, COUNTERPARTY_ID AS CounterpartyId,
                   TXN_TYPE AS TransactionType, CATEGORY, METHOD, AMOUNT, TAX_AMOUNT AS TaxAmount,
                   BUSINESS_DATE AS BusinessDate, DESCRIPTION, SOURCE_MODULE AS SourceModule, SOURCE_ID AS SourceId,
                   IS_VOIDED AS IsVoided, CREATED_AT AS CreatedAt
            FROM FIN_TRANSACTIONS
            WHERE TENANT_ID = :tenantId AND ID = :id
            """,
            new OracleParams(new { tenantId, id }));
}
