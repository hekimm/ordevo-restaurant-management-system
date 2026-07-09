using Microsoft.AspNetCore.Mvc;
using Ordevo.Web.Api;
using Ordevo.Web.Models;

namespace Ordevo.Web.Pages.Integrations;

public sealed class IndexModel(OrdevoApiClient api) : AppPageModel(api)
{
    public IReadOnlyList<ConnectorDto> Connectors { get; private set; } = [];
    public IReadOnlyList<IntegrationEventDto> Events { get; private set; } = [];
    public IReadOnlyList<TerminalCommandDto> Commands { get; private set; } = [];

    public IActionResult OnGet() => RedirectToPage("/Settings/Index", new { tab = "integrations" });
}
