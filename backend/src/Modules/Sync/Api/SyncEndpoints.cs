using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using Ordevo.BuildingBlocks.Http;
using Ordevo.BuildingBlocks.Multitenancy;
using Ordevo.Modules.Sync.Application;

namespace Ordevo.Modules.Sync.Api;

public static class SyncEndpoints
{
    private const string Read = "sync.read";
    private const string Push = "sync.push";
    private const string Manage = "sync.manage";

    public static void Map(IEndpointRouteBuilder root)
    {
        var g = root.MapGroup("/api/sync").WithTags("Sync");

        g.MapGet("/entities", async (SyncService svc, CancellationToken ct) =>
            Results.Ok(await svc.ListEntitiesAsync(ct)))
            .RequireAuthorization(Read);

        g.MapPost("/devices/register", async (RegisterDeviceRequest r, ITenantContext t, SyncService svc, CancellationToken ct) =>
            (await svc.RegisterDeviceAsync(t.RequireTenantId(), t.BranchId, r, autoApprove: false, t.UserId!, ct))
                .Match(d => Results.Created($"/api/sync/devices/{d.Id}", d)))
            .AddEndpointFilter<ValidationFilter<RegisterDeviceRequest>>()
            .RequireAuthorization(Push);

        g.MapPost("/devices/register-approved", async (RegisterDeviceRequest r, ITenantContext t, SyncService svc, CancellationToken ct) =>
            (await svc.RegisterDeviceAsync(t.RequireTenantId(), t.BranchId, r, autoApprove: true, t.UserId!, ct))
                .Match(d => Results.Created($"/api/sync/devices/{d.Id}", d)))
            .AddEndpointFilter<ValidationFilter<RegisterDeviceRequest>>()
            .RequireAuthorization(Manage);

        g.MapPost("/heartbeat", async (HeartbeatRequest r, ITenantContext t, SyncService svc, CancellationToken ct) =>
            (await svc.HeartbeatAsync(t.RequireTenantId(), t.BranchId, t.DeviceId, r, ct)).Match(Results.NoContent))
            .AddEndpointFilter<ValidationFilter<HeartbeatRequest>>()
            .RequireAuthorization(Push);

        g.MapGet("/pull", async (long? since, int? take, ITenantContext t, SyncService svc, CancellationToken ct) =>
            Results.Ok(await svc.PullAsync(t.RequireTenantId(), t.BranchId, since ?? 0, take ?? 250, ct)))
            .RequireAuthorization(Read);

        g.MapPost("/ack", async (AckPullRequest r, ITenantContext t, SyncService svc, CancellationToken ct) =>
            (await svc.AckPullAsync(t.RequireTenantId(), t.BranchId, t.DeviceId, r, ct)).Match(Results.NoContent))
            .AddEndpointFilter<ValidationFilter<AckPullRequest>>()
            .RequireAuthorization(Read);

        g.MapPost("/push", async (PushChangesRequest r, ITenantContext t, SyncService svc, CancellationToken ct) =>
            (await svc.PushAsync(t.RequireTenantId(), t.BranchId, t.DeviceId, r, t.UserId!, ct)).Match(Results.Ok))
            .AddEndpointFilter<ValidationFilter<PushChangesRequest>>()
            .RequireAuthorization(Push);

        g.MapPost("/outbox", async (AppendChangeRequest r, ITenantContext t, SyncService svc, CancellationToken ct) =>
            (await svc.AppendChangeAsync(t.RequireTenantId(), t.DeviceId, r, t.UserId!, ct)).Match(Results.Ok))
            .AddEndpointFilter<ValidationFilter<AppendChangeRequest>>()
            .RequireAuthorization(Manage);

        g.MapGet("/mutations/pending", async (int? take, ITenantContext t, SyncService svc, CancellationToken ct) =>
            Results.Ok(await svc.ListPendingMutationsAsync(t.RequireTenantId(), take ?? 100, ct)))
            .RequireAuthorization(Manage);

        g.MapGet("/conflicts/open", async (int? take, ITenantContext t, SyncService svc, CancellationToken ct) =>
            Results.Ok(await svc.ListOpenConflictsAsync(t.RequireTenantId(), take ?? 100, ct)))
            .RequireAuthorization(Manage);
    }
}
