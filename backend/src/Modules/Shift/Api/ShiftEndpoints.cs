using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using Ordevo.BuildingBlocks.Http;
using Ordevo.BuildingBlocks.Multitenancy;
using Ordevo.Modules.Shift.Application;

namespace Ordevo.Modules.Shift.Api;

public static class ShiftEndpoints
{
    private const string Manage = "shift.manage";

    private static IResult NoBranch() => Results.Problem(title: "branch.required", detail: "İşlem için şube bağlamı gerekli.", statusCode: 400);

    public static void Map(IEndpointRouteBuilder root)
    {
        var g = root.MapGroup("/api/shift").WithTags("Shift").RequireAuthorization(Manage);

        g.MapGet("/registers", async (ITenantContext t, ShiftService svc, CancellationToken ct) =>
            t.BranchId is null ? NoBranch() : Results.Ok(await svc.ListRegistersAsync(t.RequireTenantId(), t.BranchId, ct)));

        g.MapPost("/registers", async (CreateRegisterRequest r, ITenantContext t, ShiftService svc, CancellationToken ct) =>
            t.BranchId is null ? NoBranch() : (await svc.CreateRegisterAsync(t.RequireTenantId(), t.BranchId, r, ct)).Match(Results.Ok));

        g.MapPut("/registers/{id}", async (string id, UpdateRegisterRequest r, ITenantContext t, ShiftService svc, CancellationToken ct) =>
            t.BranchId is null ? NoBranch() : (await svc.UpdateRegisterAsync(t.RequireTenantId(), t.BranchId, id, r, ct)).Match(Results.Ok));

        g.MapDelete("/registers/{id}", async (string id, ITenantContext t, ShiftService svc, CancellationToken ct) =>
            t.BranchId is null ? NoBranch() : (await svc.DeleteRegisterAsync(t.RequireTenantId(), t.BranchId, id, ct)).Match(Results.NoContent));

        g.MapPost("/sessions/open", async (OpenSessionRequest r, ITenantContext t, ShiftService svc, CancellationToken ct) =>
            t.BranchId is null ? NoBranch()
                : (await svc.OpenSessionAsync(t.RequireTenantId(), t.BranchId, t.UserId!, r, ct)).Match(s => Results.Created($"/api/shift/sessions/{s.Id}", s)));

        g.MapGet("/sessions/{id}", async (string id, ITenantContext t, ShiftService svc, CancellationToken ct) =>
            (await svc.GetSessionAsync(t.RequireTenantId(), id, ct)).Match(Results.Ok));

        g.MapGet("/registers/{registerId}/open-session", async (string registerId, ITenantContext t, ShiftService svc, CancellationToken ct) =>
            (await svc.GetOpenSessionAsync(t.RequireTenantId(), registerId, ct)).Match(Results.Ok));

        g.MapPost("/sessions/{id}/pay-in", async (string id, CashMoveRequest r, ITenantContext t, ShiftService svc, CancellationToken ct) =>
            (await svc.PayInAsync(t.RequireTenantId(), id, r, t.UserId!, ct)).Match(Results.Ok));

        g.MapPost("/sessions/{id}/pay-out", async (string id, CashMoveRequest r, ITenantContext t, ShiftService svc, CancellationToken ct) =>
            (await svc.PayOutAsync(t.RequireTenantId(), id, r, t.UserId!, ct)).Match(Results.Ok));

        g.MapPost("/sessions/{id}/close", async (string id, CloseSessionRequest r, ITenantContext t, ShiftService svc, CancellationToken ct) =>
            (await svc.CloseSessionAsync(t.RequireTenantId(), id, r, t.UserId!, ct)).Match(Results.Ok));

        g.MapGet("/sessions/{id}/z-report", async (string id, ITenantContext t, ShiftService svc, CancellationToken ct) =>
            (await svc.GetZReportAsync(t.RequireTenantId(), id, ct)).Match(Results.Ok));
    }
}
