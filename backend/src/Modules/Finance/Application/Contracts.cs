namespace Ordevo.Modules.Finance.Application;

public sealed record FinanceAccountDto(
    string Id,
    string? BranchId,
    string Name,
    string AccountType,
    string Currency,
    decimal OpeningBalance,
    bool IsActive,
    DateTimeOffset CreatedAt);

public sealed record CreateFinanceAccountRequest(
    string Name,
    string AccountType,
    string? Currency,
    decimal OpeningBalance);

public sealed record UpdateFinanceAccountRequest(
    string Name,
    string AccountType,
    string? Currency,
    decimal OpeningBalance,
    bool IsActive = true);

public sealed record CounterpartyDto(
    string Id,
    string CounterpartyType,
    string? RefId,
    string Name,
    string? Phone,
    string? Email,
    string? TaxNo,
    bool IsActive,
    DateTimeOffset CreatedAt);

public sealed record CreateCounterpartyRequest(
    string CounterpartyType,
    string? RefId,
    string Name,
    string? Phone,
    string? Email,
    string? TaxNo);

public sealed record UpdateCounterpartyRequest(
    string CounterpartyType,
    string? RefId,
    string Name,
    string? Phone,
    string? Email,
    string? TaxNo,
    bool IsActive = true);

public sealed record FinanceTransactionDto(
    string Id,
    string BranchId,
    string? AccountId,
    string? CounterpartyId,
    string TransactionType,
    string Category,
    string Method,
    decimal Amount,
    decimal TaxAmount,
    DateTime BusinessDate,
    string? Description,
    string? SourceModule,
    string? SourceId,
    bool IsVoided,
    DateTimeOffset CreatedAt);

public sealed record CreateFinanceTransactionRequest(
    string? AccountId,
    string? CounterpartyId,
    string TransactionType,
    string Category,
    string Method,
    decimal Amount,
    decimal TaxAmount,
    DateTime? BusinessDate,
    string? Description);

public sealed record FinanceSummaryDto(
    string StartDate,
    string EndDate,
    decimal SalesRevenue,
    decimal OtherIncome,
    decimal Refunds,
    decimal PurchaseCosts,
    decimal Expenses,
    decimal CashIn,
    decimal CashOut,
    decimal Receivables,
    decimal Payables,
    decimal NetProfit);

public sealed record CashflowDayDto(
    string BusinessDate,
    decimal Income,
    decimal Expense,
    decimal Net);

public sealed record ProfitLossDto(
    string StartDate,
    string EndDate,
    decimal SalesRevenue,
    decimal OtherIncome,
    decimal GrossIncome,
    decimal Refunds,
    decimal PurchaseCosts,
    decimal OperatingExpenses,
    decimal NetProfit);

public sealed class FinanceAccountRow
{
    public string Id { get; set; } = default!;
    public string? BranchId { get; set; }
    public string Name { get; set; } = default!;
    public string AccountType { get; set; } = default!;
    public string Currency { get; set; } = default!;
    public decimal OpeningBalance { get; set; }
    public int IsActive { get; set; }
    public DateTimeOffset CreatedAt { get; set; }
}

public sealed class CounterpartyRow
{
    public string Id { get; set; } = default!;
    public string CounterpartyType { get; set; } = default!;
    public string? RefId { get; set; }
    public string Name { get; set; } = default!;
    public string? Phone { get; set; }
    public string? Email { get; set; }
    public string? TaxNo { get; set; }
    public int IsActive { get; set; }
    public DateTimeOffset CreatedAt { get; set; }
}

public sealed class FinanceTransactionRow
{
    public string Id { get; set; } = default!;
    public string BranchId { get; set; } = default!;
    public string? AccountId { get; set; }
    public string? CounterpartyId { get; set; }
    public string TransactionType { get; set; } = default!;
    public string Category { get; set; } = default!;
    public string Method { get; set; } = default!;
    public decimal Amount { get; set; }
    public decimal TaxAmount { get; set; }
    public DateTime BusinessDate { get; set; }
    public string? Description { get; set; }
    public string? SourceModule { get; set; }
    public string? SourceId { get; set; }
    public int IsVoided { get; set; }
    public DateTimeOffset CreatedAt { get; set; }
}

public sealed class FinanceSummaryRow
{
    public decimal SalesRevenue { get; set; }
    public decimal OtherIncome { get; set; }
    public decimal Refunds { get; set; }
    public decimal PurchaseCosts { get; set; }
    public decimal Expenses { get; set; }
    public decimal CashIn { get; set; }
    public decimal CashOut { get; set; }
    public decimal Receivables { get; set; }
    public decimal Payables { get; set; }
}

public sealed class CashflowDayRow
{
    public string BusinessDate { get; set; } = default!;
    public decimal Income { get; set; }
    public decimal Expense { get; set; }
    public decimal Net { get; set; }
}
