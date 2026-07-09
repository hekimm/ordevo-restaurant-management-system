using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using Ordevo.BuildingBlocks.Http;
using Ordevo.BuildingBlocks.Multitenancy;
using Ordevo.Modules.Fiscal.Application;

namespace Ordevo.Modules.Fiscal.Api;

public static class FiscalEndpoints
{
    private const string Read = "integration.read";
    private const string Manage = "integration.manage";
    private const string Process = "payment.process";

    public static void Map(IEndpointRouteBuilder root)
    {
        var g = root.MapGroup("/api/fiscal").WithTags("Fiscal");

        g.MapGet("/overview", async (ITenantContext t, FiscalService svc, CancellationToken ct) =>
            Results.Ok(await svc.GetOverviewAsync(t.RequireTenantId(), t.BranchId, ct)))
            .RequireAuthorization(Read);

        g.MapGet("/transactions", async (string? branchId, string? status, int? take, ITenantContext t, FiscalService svc, CancellationToken ct) =>
            Results.Ok(await svc.ListTransactionsAsync(t.RequireTenantId(), branchId ?? t.BranchId, status, take ?? 50, ct)))
            .RequireAuthorization(Read);

        g.MapPost("/orders/{orderId}/payments", async (string orderId, FiscalPaymentRequest r, ITenantContext t, FiscalService svc, CancellationToken ct) =>
            (await svc.ProcessPaymentAsync(t.RequireTenantId(), t.BranchId, orderId, t.UserId!, r, ct)).Match(Results.Ok))
            .AddEndpointFilter<ValidationFilter<FiscalPaymentRequest>>()
            .RequireAuthorization(Process);

        g.MapPost("/orders/{orderId}/manual-card-override", async (string orderId, ManualCardOverrideRequest r, ITenantContext t, FiscalService svc, CancellationToken ct) =>
            (await svc.ProcessManualCardOverrideAsync(t.RequireTenantId(), t.BranchId, orderId, t.UserId!, r, ct)).Match(Results.Ok))
            .AddEndpointFilter<ValidationFilter<ManualCardOverrideRequest>>()
            .RequireAuthorization(Manage);

        g.MapPost("/terminals/{terminalId}/test-sale", async (string terminalId, TerminalTestSaleRequest r, ITenantContext t, FiscalService svc, CancellationToken ct) =>
            (await svc.TestSaleAsync(t.RequireTenantId(), t.BranchId, terminalId, t.UserId!, r, ct)).Match(Results.Ok))
            .AddEndpointFilter<ValidationFilter<TerminalTestSaleRequest>>()
            .RequireAuthorization(Manage);
    }
}
