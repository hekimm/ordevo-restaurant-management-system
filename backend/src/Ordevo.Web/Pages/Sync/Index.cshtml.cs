using Microsoft.AspNetCore.Mvc;
using Ordevo.Web.Api;
using Ordevo.Web.Models;

namespace Ordevo.Web.Pages.Sync;

public sealed class IndexModel(OrdevoApiClient api) : AppPageModel(api)
{
    public IReadOnlyList<SyncEntityDto> Entities { get; private set; } = [];
    public IReadOnlyList<PendingMutationDto> PendingMutations { get; private set; } = [];

    public IActionResult OnGet() => RedirectToPage("/Settings/Index", new { tab = "sync" });
}
