using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using Ordevo.BuildingBlocks.Http;
using Ordevo.BuildingBlocks.Multitenancy;
using Ordevo.Modules.Print.Application;

namespace Ordevo.Modules.Print.Api;

public static class PrintEndpoints
{
    private const string Read = "print.read";
    private const string Manage = "print.manage";

    private static IResult NoBranch() => Results.Problem(title: "branch.required", detail: "İşlem için şube bağlamı gerekli.", statusCode: 400);

    public static void Map(IEndpointRouteBuilder root)
    {
        var g = root.MapGroup("/api/print").WithTags("Print");

        g.MapGet("/orders/{orderId}/receipt", async (string orderId, ITenantContext t, PrintService svc, CancellationToken ct) =>
            t.BranchId is null ? NoBranch() : (await svc.ReceiptAsync(t.RequireTenantId(), t.BranchId, orderId, ct)).Match(Results.Ok))
            .RequireAuthorization(Read);

        g.MapGet("/orders/{orderId}/kitchen-ticket", async (string orderId, ITenantContext t, PrintService svc, CancellationToken ct) =>
            t.BranchId is null ? NoBranch() : (await svc.KitchenTicketAsync(t.RequireTenantId(), t.BranchId, orderId, ct)).Match(Results.Ok))
            .RequireAuthorization(Read);

        g.MapPost("/orders/{orderId}/receipt/queue", async (string orderId, QueuePrintRequest r, ITenantContext t, PrintService svc, CancellationToken ct) =>
            t.BranchId is null ? NoBranch() : (await svc.QueueReceiptAsync(t.RequireTenantId(), t.BranchId, t.UserId!, orderId, r, ct)).Match(Results.Ok))
            .RequireAuthorization(Manage);

        g.MapPost("/orders/{orderId}/kitchen-ticket/queue", async (string orderId, QueuePrintRequest r, ITenantContext t, PrintService svc, CancellationToken ct) =>
            t.BranchId is null ? NoBranch() : (await svc.QueueKitchenTicketAsync(t.RequireTenantId(), t.BranchId, t.UserId!, orderId, r, ct)).Match(Results.Ok))
            .RequireAuthorization(Manage);

        g.MapGet("/orders/{orderId}/receipt/escpos", async (string orderId, string? business, int? width, ITenantContext t, PrintService svc, CancellationToken ct) =>
            t.BranchId is null ? NoBranch()
                : (await svc.EscPosReceiptAsync(t.RequireTenantId(), t.BranchId, orderId, string.IsNullOrWhiteSpace(business) ? "ORDEVO" : business!, width ?? 42, ct))
                    .Match(bytes => Results.File(bytes, "application/octet-stream", $"receipt-{orderId}.escpos")))
            .RequireAuthorization(Read);

        g.MapGet("/orders/{orderId}/kitchen-ticket/escpos", async (string orderId, int? width, ITenantContext t, PrintService svc, CancellationToken ct) =>
            t.BranchId is null ? NoBranch()
                : (await svc.EscPosKitchenTicketAsync(t.RequireTenantId(), t.BranchId, orderId, width ?? 42, ct))
                    .Match(bytes => Results.File(bytes, "application/octet-stream", $"kitchen-{orderId}.escpos")))
            .RequireAuthorization(Read);

        g.MapGet("/jobs", async (string? status, int? take, ITenantContext t, PrintService svc, CancellationToken ct) =>
            t.BranchId is null ? NoBranch() : Results.Ok(await svc.ListJobsAsync(t.RequireTenantId(), t.BranchId, status, take ?? 50, ct)))
            .RequireAuthorization(Read);
    }
}
