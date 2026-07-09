using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using Ordevo.BuildingBlocks.Http;
using Ordevo.BuildingBlocks.Multitenancy;
using Ordevo.Modules.EInvoice.Application;

namespace Ordevo.Modules.EInvoice.Api;

public static class EInvoiceEndpoints
{
    private const string Read = "einvoice.read";
    private const string Manage = "einvoice.manage";

    public static void Map(IEndpointRouteBuilder root)
    {
        var g = root.MapGroup("/api/einvoice").WithTags("EInvoice");

        g.MapPost("/orders/{orderId}/issue", async (
                string orderId, IssueEInvoiceRequest r, ITenantContext t, EInvoiceService svc, CancellationToken ct) =>
            (await svc.IssueForOrderAsync(t.RequireTenantId(), t.UserId, orderId, r, ct))
                .Match(x => Results.Created($"/api/einvoice/documents/{x.Id}", x)))
            .RequireAuthorization(Manage);

        g.MapGet("/documents", async (string? orderId, string? status, ITenantContext t, EInvoiceService svc, CancellationToken ct) =>
            Results.Ok(await svc.ListAsync(t.RequireTenantId(), orderId, status, ct)))
            .RequireAuthorization(Read);

        g.MapGet("/documents/{id}", async (string id, ITenantContext t, EInvoiceService svc, CancellationToken ct) =>
            (await svc.GetAsync(t.RequireTenantId(), id, ct)).Match(Results.Ok))
            .RequireAuthorization(Read);

        g.MapGet("/orders/{orderId}", async (string orderId, ITenantContext t, EInvoiceService svc, CancellationToken ct) =>
            Results.Ok(await svc.ListAsync(t.RequireTenantId(), orderId, null, ct)))
            .RequireAuthorization(Read);

        g.MapPost("/documents/{id}/refresh-status", async (string id, ITenantContext t, EInvoiceService svc, CancellationToken ct) =>
            (await svc.RefreshStatusAsync(t.RequireTenantId(), id, ct)).Match(Results.Ok))
            .RequireAuthorization(Manage);

        g.MapPost("/documents/{id}/cancel", async (string id, CancelEInvoiceRequest r, ITenantContext t, EInvoiceService svc, CancellationToken ct) =>
            (await svc.CancelAsync(t.RequireTenantId(), id, r.Reason ?? "", ct)).Match(Results.Ok))
            .RequireAuthorization(Manage);
    }
}
