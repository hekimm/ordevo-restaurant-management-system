using Ordevo.BuildingBlocks.Results;
using Ordevo.Modules.Finance.Infrastructure;

namespace Ordevo.Modules.Finance.Application;

public sealed class FinanceService(IFinanceRepository repo)
{
    private static readonly HashSet<string> AccountTypes = new(StringComparer.OrdinalIgnoreCase)
    {
        "cash", "bank", "card", "online", "supplier", "customer", "other"
    };

    private static readonly HashSet<string> CounterpartyTypes = new(StringComparer.OrdinalIgnoreCase)
    {
        "customer", "supplier", "staff", "other"
    };

    private static readonly HashSet<string> TransactionTypes = new(StringComparer.OrdinalIgnoreCase)
    {
        "income", "expense", "adjustment"
    };

    private static readonly HashSet<string> Methods = new(StringComparer.OrdinalIgnoreCase)
    {
        "cash", "card", "bank", "online", "meal_voucher", "on_account", "other"
    };

    public async Task<IReadOnlyList<FinanceAccountDto>> ListAccountsAsync(string tenantId, string? branchId, CancellationToken ct = default)
        => (await repo.ListAccountsAsync(tenantId, branchId, ct)).Select(Map).ToList();

    public async Task<Result<FinanceAccountDto>> CreateAccountAsync(string tenantId, string? branchId, CreateFinanceAccountRequest request, CancellationToken ct = default)
    {
        var name = (request.Name ?? "").Trim();
        var accountType = Normalize(request.AccountType);
        var currency = string.IsNullOrWhiteSpace(request.Currency) ? "TRY" : request.Currency.Trim().ToUpperInvariant();

        if (name.Length is 0 or > 160)
            return Error.Validation("finance.account.name", "Hesap adı 1-160 karakter olmalı.");
        if (!AccountTypes.Contains(accountType))
            return Error.Validation("finance.account.type", "Geçersiz finans hesap tipi.");
        if (currency.Length != 3)
            return Error.Validation("finance.account.currency", "Para birimi 3 karakter olmalı.");

        var row = await repo.CreateAccountAsync(
            tenantId,
            branchId,
            new CreateFinanceAccountRequest(name, accountType, currency, request.OpeningBalance),
            ct);
        return Map(row);
    }

    public async Task<Result<FinanceAccountDto>> UpdateAccountAsync(string tenantId, string id, UpdateFinanceAccountRequest request, CancellationToken ct = default)
    {
        var name = (request.Name ?? "").Trim();
        var accountType = Normalize(request.AccountType);
        var currency = string.IsNullOrWhiteSpace(request.Currency) ? "TRY" : request.Currency.Trim().ToUpperInvariant();

        if (name.Length is 0 or > 160)
            return Error.Validation("finance.account.name", "Hesap adı 1-160 karakter olmalı.");
        if (!AccountTypes.Contains(accountType))
            return Error.Validation("finance.account.type", "Geçersiz finans hesap tipi.");
        if (currency.Length != 3)
            return Error.Validation("finance.account.currency", "Para birimi 3 karakter olmalı.");

        var row = await repo.UpdateAccountAsync(
            tenantId,
            id,
            new UpdateFinanceAccountRequest(name, accountType, currency, request.OpeningBalance, request.IsActive),
            ct);
        return row is null ? Error.NotFound("finance.account.not_found", "Hesap bulunamadı.") : Map(row);
    }

    public async Task<Result> DeactivateAccountAsync(string tenantId, string id, CancellationToken ct = default)
    {
        var affected = await repo.DeactivateAccountAsync(tenantId, id, ct);
        return affected > 0 ? Result.Success() : Error.NotFound("finance.account.not_found", "Hesap bulunamadı.");
    }

    public async Task<IReadOnlyList<CounterpartyDto>> ListCounterpartiesAsync(string tenantId, string? type, CancellationToken ct = default)
        => (await repo.ListCounterpartiesAsync(tenantId, NormalizeNullable(type), ct)).Select(Map).ToList();

    public async Task<Result<CounterpartyDto>> CreateCounterpartyAsync(string tenantId, CreateCounterpartyRequest request, CancellationToken ct = default)
    {
        var type = Normalize(request.CounterpartyType);
        var name = (request.Name ?? "").Trim();

        if (!CounterpartyTypes.Contains(type))
            return Error.Validation("finance.counterparty.type", "Geçersiz cari tipi.");
        if (name.Length is 0 or > 200)
            return Error.Validation("finance.counterparty.name", "Cari adı 1-200 karakter olmalı.");

        var row = await repo.CreateCounterpartyAsync(
            tenantId,
            new CreateCounterpartyRequest(type, Clean(request.RefId), name, Clean(request.Phone), Clean(request.Email), Clean(request.TaxNo)),
            ct);
        return Map(row);
    }

    public async Task<Result<CounterpartyDto>> UpdateCounterpartyAsync(string tenantId, string id, UpdateCounterpartyRequest request, CancellationToken ct = default)
    {
        var type = Normalize(request.CounterpartyType);
        var name = (request.Name ?? "").Trim();

        if (!CounterpartyTypes.Contains(type))
            return Error.Validation("finance.counterparty.type", "Geçersiz cari tipi.");
        if (name.Length is 0 or > 200)
            return Error.Validation("finance.counterparty.name", "Cari adı 1-200 karakter olmalı.");

        var row = await repo.UpdateCounterpartyAsync(
            tenantId,
            id,
            new UpdateCounterpartyRequest(type, Clean(request.RefId), name, Clean(request.Phone), Clean(request.Email), Clean(request.TaxNo), request.IsActive),
            ct);
        return row is null ? Error.NotFound("finance.counterparty.not_found", "Cari bulunamadı.") : Map(row);
    }

    public async Task<Result> DeactivateCounterpartyAsync(string tenantId, string id, CancellationToken ct = default)
    {
        var affected = await repo.DeactivateCounterpartyAsync(tenantId, id, ct);
        return affected > 0 ? Result.Success() : Error.NotFound("finance.counterparty.not_found", "Cari bulunamadı.");
    }

    public async Task<IReadOnlyList<FinanceTransactionDto>> ListTransactionsAsync(string tenantId, string branchId, DateTime start, DateTime end, string? type, CancellationToken ct = default)
        => (await repo.ListTransactionsAsync(tenantId, branchId, start.Date, end.Date, NormalizeNullable(type), ct)).Select(Map).ToList();

    public async Task<Result<FinanceTransactionDto>> CreateTransactionAsync(string tenantId, string branchId, string userId, CreateFinanceTransactionRequest request, CancellationToken ct = default)
    {
        var transactionType = Normalize(request.TransactionType);
        var method = Normalize(request.Method);
        var category = string.IsNullOrWhiteSpace(request.Category) ? "genel" : request.Category.Trim();
        var businessDate = (request.BusinessDate ?? DateTime.UtcNow).Date;

        if (!TransactionTypes.Contains(transactionType))
            return Error.Validation("finance.transaction.type", "Gelir/gider işlem tipi geçersiz.");
        if (!Methods.Contains(method))
            return Error.Validation("finance.transaction.method", "Ödeme/tahsilat yöntemi geçersiz.");
        if (request.Amount <= 0)
            return Error.Validation("finance.transaction.amount", "Tutar sıfırdan büyük olmalı.");
        if (request.TaxAmount < 0)
            return Error.Validation("finance.transaction.tax", "Vergi tutarı negatif olamaz.");
        if (category.Length > 120)
            return Error.Validation("finance.transaction.category", "Kategori 120 karakteri aşmamalı.");

        var row = await repo.CreateTransactionAsync(
            tenantId,
            branchId,
            userId,
            new CreateFinanceTransactionRequest(
                Clean(request.AccountId),
                Clean(request.CounterpartyId),
                transactionType,
                category,
                method,
                request.Amount,
                request.TaxAmount,
                businessDate,
                Clean(request.Description)),
            ct);
        return Map(row);
    }

    public async Task<Result<FinanceTransactionDto>> UpdateTransactionAsync(string tenantId, string branchId, string id, CreateFinanceTransactionRequest request, CancellationToken ct = default)
    {
        var prepared = PrepareTransaction(request);
        if (!prepared.IsSuccess)
            return prepared.Error;

        var row = await repo.UpdateTransactionAsync(tenantId, branchId, id, prepared.Value, ct);
        return row is null ? Error.NotFound("finance.transaction.not_found", "Hareket bulunamadı veya düzenlenemez.") : Map(row);
    }

    public async Task<Result> VoidTransactionAsync(string tenantId, string branchId, string id, CancellationToken ct = default)
    {
        var affected = await repo.VoidTransactionAsync(tenantId, branchId, id, ct);
        return affected > 0 ? Result.Success() : Error.NotFound("finance.transaction.not_found", "Hareket bulunamadı veya zaten iptal edilmiş.");
    }

    public async Task<FinanceSummaryDto> SummaryAsync(string tenantId, string branchId, DateTime start, DateTime end, CancellationToken ct = default)
    {
        var row = await repo.SummaryAsync(tenantId, branchId, start.Date, end.Date, ct);
        var netProfit = row.SalesRevenue + row.OtherIncome - row.Refunds - row.PurchaseCosts - row.Expenses;
        return new FinanceSummaryDto(
            start.ToString("yyyy-MM-dd"),
            end.ToString("yyyy-MM-dd"),
            row.SalesRevenue,
            row.OtherIncome,
            row.Refunds,
            row.PurchaseCosts,
            row.Expenses,
            row.CashIn,
            row.CashOut,
            row.Receivables,
            row.Payables,
            netProfit);
    }

    public async Task<IReadOnlyList<CashflowDayDto>> CashflowAsync(string tenantId, string branchId, DateTime start, DateTime end, CancellationToken ct = default)
        => (await repo.CashflowAsync(tenantId, branchId, start.Date, end.Date, ct))
            .Select(x => new CashflowDayDto(x.BusinessDate, x.Income, x.Expense, x.Net))
            .ToList();

    public async Task<ProfitLossDto> ProfitLossAsync(string tenantId, string branchId, DateTime start, DateTime end, CancellationToken ct = default)
    {
        var row = await repo.SummaryAsync(tenantId, branchId, start.Date, end.Date, ct);
        var grossIncome = row.SalesRevenue + row.OtherIncome;
        var netProfit = grossIncome - row.Refunds - row.PurchaseCosts - row.Expenses;
        return new ProfitLossDto(
            start.ToString("yyyy-MM-dd"),
            end.ToString("yyyy-MM-dd"),
            row.SalesRevenue,
            row.OtherIncome,
            grossIncome,
            row.Refunds,
            row.PurchaseCosts,
            row.Expenses,
            netProfit);
    }

    private static FinanceAccountDto Map(FinanceAccountRow x)
        => new(x.Id, x.BranchId, x.Name, x.AccountType, x.Currency, x.OpeningBalance, x.IsActive == 1, x.CreatedAt);

    private static CounterpartyDto Map(CounterpartyRow x)
        => new(x.Id, x.CounterpartyType, x.RefId, x.Name, x.Phone, x.Email, x.TaxNo, x.IsActive == 1, x.CreatedAt);

    private static FinanceTransactionDto Map(FinanceTransactionRow x)
        => new(x.Id, x.BranchId, x.AccountId, x.CounterpartyId, x.TransactionType, x.Category, x.Method, x.Amount, x.TaxAmount, x.BusinessDate, x.Description, x.SourceModule, x.SourceId, x.IsVoided == 1, x.CreatedAt);

    private Result<CreateFinanceTransactionRequest> PrepareTransaction(CreateFinanceTransactionRequest request)
    {
        var transactionType = Normalize(request.TransactionType);
        var method = Normalize(request.Method);
        var category = string.IsNullOrWhiteSpace(request.Category) ? "genel" : request.Category.Trim();
        var businessDate = (request.BusinessDate ?? DateTime.UtcNow).Date;

        if (!TransactionTypes.Contains(transactionType))
            return Error.Validation("finance.transaction.type", "Gelir/gider işlem tipi geçersiz.");
        if (!Methods.Contains(method))
            return Error.Validation("finance.transaction.method", "Ödeme/tahsilat yöntemi geçersiz.");
        if (request.Amount <= 0)
            return Error.Validation("finance.transaction.amount", "Tutar sıfırdan büyük olmalı.");
        if (request.TaxAmount < 0)
            return Error.Validation("finance.transaction.tax", "Vergi tutarı negatif olamaz.");
        if (category.Length > 120)
            return Error.Validation("finance.transaction.category", "Kategori 120 karakteri aşmamalı.");

        return new CreateFinanceTransactionRequest(
            Clean(request.AccountId),
            Clean(request.CounterpartyId),
            transactionType,
            category,
            method,
            request.Amount,
            request.TaxAmount,
            businessDate,
            Clean(request.Description));
    }

    private static string Normalize(string? value) => (value ?? "").Trim().ToLowerInvariant();
    private static string? NormalizeNullable(string? value) => string.IsNullOrWhiteSpace(value) ? null : Normalize(value);
    private static string? Clean(string? value) => string.IsNullOrWhiteSpace(value) ? null : value.Trim();
}
