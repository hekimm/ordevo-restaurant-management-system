using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Mvc;
using Ordevo.Web.Api;
using Ordevo.Web.Models;

namespace Ordevo.Web.Pages.Kitchen;

public sealed class IndexModel(OrdevoApiClient api, IConfiguration config) : AppPageModel(api)
{
    public IReadOnlyList<KdsTicketDto> Tickets { get; private set; } = [];
    public string HubBase { get; private set; } = "";
    public string HubToken { get; private set; } = "";

    public async Task OnGetAsync(CancellationToken ct)
    {
        Tickets = await GetListAsync<KdsTicketDto>("/api/kitchen/board", ct);
        HubBase = (config["OrdevoApi:BaseUrl"] ?? "").TrimEnd('/');
        HubToken = await HttpContext.GetTokenAsync("access_token") ?? "";
    }

    public async Task<IActionResult> OnGetBoardAsync(string? station, CancellationToken ct)
    {
        var q = string.IsNullOrWhiteSpace(station) ? "" : $"?station={Uri.EscapeDataString(station)}";
        var r = await Api.GetAsync<List<KdsTicketDto>>($"/api/kitchen/board{q}", ct);
        return r.IsSuccess ? new JsonResult(r.Value) : BadRequest(new { error = UiFormat.Error(r) });
    }

    public async Task<IActionResult> OnPostItemStatusAsync([FromBody] ItemStatusReq body, CancellationToken ct)
    {
        var r = await Api.PostAsync<object>($"/api/kitchen/items/{body.ItemId}/status", new { status = body.Status }, ct);
        return r.IsSuccess ? new JsonResult(new { ok = true }) : BadRequest(new { error = UiFormat.Error(r) });
    }

    public sealed record ItemStatusReq(string ItemId, string Status);
}
