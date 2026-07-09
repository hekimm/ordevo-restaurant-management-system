using Microsoft.AspNetCore.Mvc;
using Ordevo.Web.Api;
using Ordevo.Web.Models;

namespace Ordevo.Web.Pages.Finance;

public sealed class IndexModel(OrdevoApiClient api) : AppPageModel(api)
{
    [BindProperty(SupportsGet = true)]
    public string? Start { get; set; }

    [BindProperty(SupportsGet = true)]
    public string? End { get; set; }

    [BindProperty]
    public TransactionInput Transaction { get; set; } = new();

    [BindProperty]
    public AccountInput Account { get; set; } = new();

    [BindProperty]
    public CounterpartyInput Counterparty { get; set; } = new();

    public FinanceSummaryDto? Summary { get; private set; }
    public IReadOnlyList<CashflowDayDto> Cashflow { get; private set; } = [];
    public IReadOnlyList<FinanceTransactionDto> Transactions { get; private set; } = [];
    public IReadOnlyList<FinanceAccountDto> Accounts { get; private set; } = [];
    public IReadOnlyList<CounterpartyDto> Counterparties { get; private set; } = [];

    public async Task OnGetAsync(CancellationToken ct)
    {
        EnsureRange();
        await LoadAsync(ct);
    }

    public async Task<IActionResult> OnPostTransactionAsync(CancellationToken ct)
    {
        EnsureRange();
        var body = new
        {
            Transaction.AccountId,
            Transaction.CounterpartyId,
            Transaction.TransactionType,
            Transaction.Category,
            Transaction.Method,
            Transaction.Amount,
            Transaction.TaxAmount,
            BusinessDate = Transaction.BusinessDate,
            Transaction.Description
        };

        var result = string.IsNullOrWhiteSpace(Transaction.Id)
            ? await Api.PostAsync<FinanceTransactionDto>("/api/finance/transactions", body, ct)
            : await Api.PutAsync<FinanceTransactionDto>($"/api/finance/transactions/{Transaction.Id}", body, ct);

        if (result.IsSuccess)
        {
            NotifySuccess("Finans hareketi kaydedildi.");
            return RedirectToPage(new { start = Start, end = End });
        }

        Errors.Add(UiFormat.Error(result));
        await LoadAsync(ct);
        return Page();
    }

    public async Task<IActionResult> OnPostAccountAsync(CancellationToken ct)
    {
        EnsureRange();
        var body = new
        {
            Account.Name,
            Account.AccountType,
            Account.Currency,
            Account.OpeningBalance,
            Account.IsActive
        };

        var result = string.IsNullOrWhiteSpace(Account.Id)
            ? await Api.PostAsync<FinanceAccountDto>("/api/finance/accounts", body, ct)
            : await Api.PutAsync<FinanceAccountDto>($"/api/finance/accounts/{Account.Id}", body, ct);

        if (result.IsSuccess)
        {
            NotifySuccess("Finans hesabı kaydedildi.");
            return RedirectToPage(new { start = Start, end = End });
        }

        Errors.Add(UiFormat.Error(result));
        await LoadAsync(ct);
        return Page();
    }

    public async Task<IActionResult> OnPostCounterpartyAsync(CancellationToken ct)
    {
        EnsureRange();
        var body = new
        {
            Counterparty.CounterpartyType,
            Counterparty.RefId,
            Counterparty.Name,
            Counterparty.Phone,
            Counterparty.Email,
            Counterparty.TaxNo,
            Counterparty.IsActive
        };

        var result = string.IsNullOrWhiteSpace(Counterparty.Id)
            ? await Api.PostAsync<CounterpartyDto>("/api/finance/counterparties", body, ct)
            : await Api.PutAsync<CounterpartyDto>($"/api/finance/counterparties/{Counterparty.Id}", body, ct);

        if (result.IsSuccess)
        {
            NotifySuccess("Cari bilgisi kaydedildi.");
            return RedirectToPage(new { start = Start, end = End });
        }

        Errors.Add(UiFormat.Error(result));
        await LoadAsync(ct);
        return Page();
    }

    public async Task<IActionResult> OnPostVoidTransactionAsync(string id, CancellationToken ct)
    {
        EnsureRange();
        var result = await Api.DeleteAsync<string>($"/api/finance/transactions/{id}", ct);
        return await CompleteDeleteAsync(result, ct);
    }

    public async Task<IActionResult> OnPostDeleteAccountAsync(string id, CancellationToken ct)
    {
        EnsureRange();
        var result = await Api.DeleteAsync<string>($"/api/finance/accounts/{id}", ct);
        return await CompleteDeleteAsync(result, ct);
    }

    public async Task<IActionResult> OnPostDeleteCounterpartyAsync(string id, CancellationToken ct)
    {
        EnsureRange();
        var result = await Api.DeleteAsync<string>($"/api/finance/counterparties/{id}", ct);
        return await CompleteDeleteAsync(result, ct);
    }

    private async Task<IActionResult> CompleteDeleteAsync<T>(ApiResult<T> result, CancellationToken ct)
    {
        if (result.IsSuccess)
        {
            NotifySuccess("Finans işlemi tamamlandı.");
            return RedirectToPage(new { start = Start, end = End });
        }

        Errors.Add(UiFormat.Error(result));
        await LoadAsync(ct);
        return Page();
    }

    private async Task LoadAsync(CancellationToken ct)
    {
        var query = $"?start={Uri.EscapeDataString(Start!)}&end={Uri.EscapeDataString(End!)}";
        Summary = await GetOneAsync<FinanceSummaryDto>($"/api/finance/summary{query}", ct);
        Cashflow = await GetListAsync<CashflowDayDto>($"/api/finance/cashflow{query}", ct);
        Transactions = await GetListAsync<FinanceTransactionDto>($"/api/finance/transactions{query}", ct);
        Accounts = await GetListAsync<FinanceAccountDto>("/api/finance/accounts", ct);
        Counterparties = await GetListAsync<CounterpartyDto>("/api/finance/counterparties", ct);

        Transaction.BusinessDate ??= DateTime.UtcNow.Date;
        Transaction.Method = string.IsNullOrWhiteSpace(Transaction.Method) ? "cash" : Transaction.Method;
        Transaction.TransactionType = string.IsNullOrWhiteSpace(Transaction.TransactionType) ? "expense" : Transaction.TransactionType;
        Transaction.Category = string.IsNullOrWhiteSpace(Transaction.Category) ? "Genel" : Transaction.Category;
        Account.Currency = string.IsNullOrWhiteSpace(Account.Currency) ? "TRY" : Account.Currency;
        Account.IsActive = true;
        Counterparty.IsActive = true;
    }

    private void EnsureRange()
    {
        var today = DateTime.UtcNow.Date;
        if (!DateTime.TryParse(Start, out var start))
            start = today.AddDays(-30);
        if (!DateTime.TryParse(End, out var end))
            end = today;

        Start = start.ToString("yyyy-MM-dd");
        End = end.ToString("yyyy-MM-dd");
    }

    public sealed class TransactionInput
    {
        public string? Id { get; set; }
        public string? AccountId { get; set; }
        public string? CounterpartyId { get; set; }
        public string TransactionType { get; set; } = "expense";
        public string Category { get; set; } = "Genel";
        public string Method { get; set; } = "cash";
        public decimal Amount { get; set; }
        public decimal TaxAmount { get; set; }
        public DateTime? BusinessDate { get; set; }
        public string? Description { get; set; }
    }

    public sealed class AccountInput
    {
        public string? Id { get; set; }
        public string Name { get; set; } = "";
        public string AccountType { get; set; } = "cash";
        public string Currency { get; set; } = "TRY";
        public decimal OpeningBalance { get; set; }
        public bool IsActive { get; set; } = true;
    }

    public sealed class CounterpartyInput
    {
        public string? Id { get; set; }
        public string CounterpartyType { get; set; } = "supplier";
        public string? RefId { get; set; }
        public string Name { get; set; } = "";
        public string? Phone { get; set; }
        public string? Email { get; set; }
        public string? TaxNo { get; set; }
        public bool IsActive { get; set; } = true;
    }
}
