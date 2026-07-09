using Microsoft.AspNetCore.Mvc;
using Ordevo.Web.Api;
using Ordevo.Web.Models;

namespace Ordevo.Web.Pages.Crm;

public sealed class IndexModel(OrdevoApiClient api) : AppPageModel(api)
{
    [BindProperty(SupportsGet = true)]
    public string? Search { get; set; }

    [BindProperty(SupportsGet = true)]
    public string? Date { get; set; }

    [BindProperty]
    public CustomerInput Customer { get; set; } = new();

    [BindProperty]
    public ReservationInput Reservation { get; set; } = new();

    public IReadOnlyList<CustomerDto> Customers { get; private set; } = [];
    public IReadOnlyList<ReservationDto> Reservations { get; private set; } = [];
    public IReadOnlyList<TableDto> Tables { get; private set; } = [];

    public async Task OnGetAsync(CancellationToken ct)
    {
        EnsureDate();
        await LoadAsync(ct);
    }

    public async Task<IActionResult> OnPostCustomerAsync(CancellationToken ct)
    {
        EnsureDate();

        var result = string.IsNullOrWhiteSpace(Customer.Id)
            ? await Api.PostAsync<CustomerDto>("/api/m9-crm/customers", new
            {
                Customer.Phone,
                Customer.FullName,
                Customer.Email,
                Customer.Birthday,
                Customer.SmsConsent,
                Customer.EmailConsent
            }, ct)
            : await Api.PutAsync<CustomerDto>($"/api/m9-crm/customers/{Customer.Id}", new
            {
                Customer.FullName,
                Customer.Email,
                Customer.Birthday,
                Customer.Notes,
                Customer.Preferences,
                Customer.SmsConsent,
                Customer.EmailConsent
            }, ct);

        return await CompleteMutationAsync(result, ct);
    }

    public async Task<IActionResult> OnPostBlockCustomerAsync(string id, string? reason, CancellationToken ct)
    {
        EnsureDate();
        var result = await Api.PostAsync<string>($"/api/m9-crm/customers/{id}/block", new { reason = reason ?? "Panel" }, ct);
        return await CompleteMutationAsync(result, ct);
    }

    public async Task<IActionResult> OnPostUnblockCustomerAsync(string id, CancellationToken ct)
    {
        EnsureDate();
        var result = await Api.PostAsync<string>($"/api/m9-crm/customers/{id}/unblock", new { }, ct);
        return await CompleteMutationAsync(result, ct);
    }

    public async Task<IActionResult> OnPostDeleteCustomerAsync(string id, CancellationToken ct)
    {
        EnsureDate();
        var result = await Api.DeleteAsync<string>($"/api/m9-crm/customers/{id}", ct);
        return await CompleteMutationAsync(result, ct);
    }

    public async Task<IActionResult> OnPostReservationAsync(CancellationToken ct)
    {
        EnsureDate();
        var body = new
        {
            CustomerId = string.IsNullOrWhiteSpace(Reservation.CustomerId) ? null : Reservation.CustomerId,
            Reservation.CustomerName,
            Reservation.CustomerPhone,
            Reservation.ReservationDate,
            Reservation.ReservationTime,
            Reservation.GuestCount,
            TableId = string.IsNullOrWhiteSpace(Reservation.TableId) ? null : Reservation.TableId,
            Reservation.Notes
        };

        var result = string.IsNullOrWhiteSpace(Reservation.Id)
            ? await Api.PostAsync<ReservationDto>("/api/m9-crm/reservations", body, ct)
            : await Api.PutAsync<ReservationDto>($"/api/m9-crm/reservations/{Reservation.Id}", body, ct);

        return await CompleteMutationAsync(result, ct);
    }

    public async Task<IActionResult> OnPostReservationStatusAsync(string id, string status, string? reason, CancellationToken ct)
    {
        EnsureDate();
        var result = await Api.PutAsync<ReservationDto>($"/api/m9-crm/reservations/{id}/status", new { status, reason }, ct);
        return await CompleteMutationAsync(result, ct);
    }

    public async Task<IActionResult> OnPostDeleteReservationAsync(string id, CancellationToken ct)
    {
        EnsureDate();
        var result = await Api.DeleteAsync<string>($"/api/m9-crm/reservations/{id}", ct);
        return await CompleteMutationAsync(result, ct);
    }

    private async Task LoadAsync(CancellationToken ct)
    {
        var customerPath = string.IsNullOrWhiteSpace(Search)
            ? "/api/m9-crm/customers?take=50"
            : $"/api/m9-crm/customers?search={Uri.EscapeDataString(Search)}&take=50";
        Customers = await GetListAsync<CustomerDto>(customerPath, ct);
        Reservations = await GetListAsync<ReservationDto>($"/api/m9-crm/reservations?date={Uri.EscapeDataString(Date!)}", ct);
        Tables = await GetListAsync<TableDto>("/api/ordering/tables", ct);

        Customer.SmsConsent = true;
        Customer.EmailConsent = true;
        Reservation.ReservationDate ??= DateTime.Parse(Date!);
        Reservation.ReservationTime = string.IsNullOrWhiteSpace(Reservation.ReservationTime) ? "19:00" : Reservation.ReservationTime;
        Reservation.GuestCount = Reservation.GuestCount == 0 ? 2 : Reservation.GuestCount;
    }

    private async Task<IActionResult> CompleteMutationAsync<T>(ApiResult<T> result, CancellationToken ct)
    {
        if (result.IsSuccess)
        {
            NotifySuccess("İşlem tamamlandı.");
            return RedirectToPage(new { search = Search, date = Date });
        }

        Errors.Add(UiFormat.Error(result));
        await LoadAsync(ct);
        return Page();
    }

    private void EnsureDate()
    {
        if (!DateTime.TryParse(Date, out var date))
            date = DateTime.UtcNow.Date;

        Date = date.ToString("yyyy-MM-dd");
    }

    public sealed class CustomerInput
    {
        public string? Id { get; set; }
        public string Phone { get; set; } = "";
        public string? FullName { get; set; }
        public string? Email { get; set; }
        public DateTime? Birthday { get; set; }
        public string? Notes { get; set; }
        public string? Preferences { get; set; }
        public bool SmsConsent { get; set; } = true;
        public bool EmailConsent { get; set; } = true;
    }

    public sealed class ReservationInput
    {
        public string? Id { get; set; }
        public string? CustomerId { get; set; }
        public string CustomerName { get; set; } = "";
        public string CustomerPhone { get; set; } = "";
        public DateTime? ReservationDate { get; set; }
        public string ReservationTime { get; set; } = "19:00";
        public int GuestCount { get; set; } = 2;
        public string? TableId { get; set; }
        public string? Notes { get; set; }
    }
}
