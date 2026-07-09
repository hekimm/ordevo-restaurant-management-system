using Microsoft.AspNetCore.Mvc;
using Ordevo.Web.Api;
using Ordevo.Web.Models;

namespace Ordevo.Web.Pages.Shift;

public sealed class IndexModel(OrdevoApiClient api) : AppPageModel(api)
{
    public IReadOnlyList<UserSummaryDto> Users { get; private set; } = [];

    public IReadOnlyList<UserSummaryDto> Waiters => Users
        .Where(x => x.Roles.Any(r => string.Equals(r, "waiter", StringComparison.OrdinalIgnoreCase)))
        .OrderByDescending(x => x.IsActive)
        .ThenBy(x => x.FullName)
        .ToList();

    [BindProperty]
    public CreateWaiterInput Waiter { get; set; } = new();

    [BindProperty]
    public UpdateWaiterInput Staff { get; set; } = new();

    [BindProperty]
    public PinInput Pin { get; set; } = new();

    public async Task OnGetAsync(CancellationToken ct)
    {
        await LoadAsync(ct);
    }

    public async Task<IActionResult> OnPostCreateWaiterAsync(CancellationToken ct)
    {
        var result = await Api.PostAsync<UserSummaryDto>("/api/identity/users/waiters", new
        {
            Waiter.FullName,
            Waiter.Pin,
            IsActive = true
        }, ct);

        return await CompleteMutationAsync(result, "Garson mobil erişimi oluşturuldu.", ct);
    }

    public async Task<IActionResult> OnPostUpdateWaiterAsync(string id, CancellationToken ct)
    {
        var result = await Api.PutAsync<UserSummaryDto>($"/api/identity/users/{id}", new
        {
            Staff.FullName,
            Staff.IsActive,
            Roles = new[] { "waiter" },
            BranchIds = (string[]?)null
        }, ct);

        return await CompleteMutationAsync(result, "Personel bilgileri güncellendi.", ct);
    }

    public async Task<IActionResult> OnPostResetPinAsync(string id, CancellationToken ct)
    {
        var result = await Api.PostAsync<string>($"/api/identity/users/{id}/pin", new
        {
            Pin.Pin
        }, ct);

        return await CompleteMutationAsync(result, "Mobil PIN yenilendi.", ct);
    }

    public async Task<IActionResult> OnPostDeactivateWaiterAsync(string id, CancellationToken ct)
    {
        var result = await Api.DeleteAsync<string>($"/api/identity/users/{id}", ct);
        return await CompleteMutationAsync(result, "Garson erişimi pasifleştirildi.", ct);
    }

    private async Task LoadAsync(CancellationToken ct)
    {
        Users = await GetListAsync<UserSummaryDto>("/api/identity/users", ct);
    }

    private async Task<IActionResult> CompleteMutationAsync<T>(ApiResult<T> result, string successMessage, CancellationToken ct)
    {
        if (result.IsSuccess)
        {
            NotifySuccess(successMessage);
            return RedirectToPage();
        }

        var message = UiFormat.Error(result);
        Errors.Add(message);
        NotifyError(message);
        await LoadAsync(ct);
        return Page();
    }

    public sealed class CreateWaiterInput
    {
        public string FullName { get; set; } = "";
        public string Pin { get; set; } = "";
    }

    public sealed class UpdateWaiterInput
    {
        public string FullName { get; set; } = "";
        public bool IsActive { get; set; } = true;
    }

    public sealed class PinInput
    {
        public string Pin { get; set; } = "";
    }
}
