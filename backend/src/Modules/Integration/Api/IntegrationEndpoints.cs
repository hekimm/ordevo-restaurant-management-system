using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using Ordevo.BuildingBlocks.Http;
using Ordevo.BuildingBlocks.Multitenancy;
using Ordevo.Modules.Integration.Application;

namespace Ordevo.Modules.Integration.Api;

public static class IntegrationEndpoints
{
    private const string Read = "integration.read";
    private const string Manage = "integration.manage";
    private const string Dispatch = "integration.dispatch";
    private const string Terminal = "integration.terminal";

    public static void Map(IEndpointRouteBuilder root)
    {
        var g = root.MapGroup("/api/integrations").WithTags("Integration");

        g.MapGet("/connectors", async (string? branchId, string? connectorType, ITenantContext t, IntegrationService svc, CancellationToken ct) =>
            Results.Ok(await svc.ListConnectorsAsync(t.RequireTenantId(), branchId ?? t.BranchId, connectorType, ct)))
            .RequireAuthorization(Read);

        g.MapPost("/connectors", async (CreateConnectorRequest r, ITenantContext t, IntegrationService svc, CancellationToken ct) =>
            (await svc.CreateConnectorAsync(t.RequireTenantId(), t.BranchId, r, t.UserId!, ct))
                .Match(c => Results.Created($"/api/integrations/connectors/{c.Id}", c)))
            .AddEndpointFilter<ValidationFilter<CreateConnectorRequest>>()
            .RequireAuthorization(Manage);

        g.MapPost("/connectors/{connectorId}/status", async (string connectorId, SetConnectorStatusRequest r, ITenantContext t, IntegrationService svc, CancellationToken ct) =>
            (await svc.SetConnectorStatusAsync(t.RequireTenantId(), connectorId, r, t.UserId!, ct)).Match(Results.NoContent))
            .AddEndpointFilter<ValidationFilter<SetConnectorStatusRequest>>()
            .RequireAuthorization(Manage);

        g.MapGet("/webhooks/subscriptions", async (string? branchId, ITenantContext t, IntegrationService svc, CancellationToken ct) =>
            Results.Ok(await svc.ListWebhookSubscriptionsAsync(t.RequireTenantId(), branchId ?? t.BranchId, ct)))
            .RequireAuthorization(Read);

        g.MapPost("/webhooks/subscriptions", async (CreateWebhookSubscriptionRequest r, ITenantContext t, IntegrationService svc, CancellationToken ct) =>
            (await svc.CreateWebhookSubscriptionAsync(t.RequireTenantId(), t.BranchId, r, t.UserId!, ct))
                .Match(s => Results.Created($"/api/integrations/webhooks/subscriptions/{s.Id}", s)))
            .AddEndpointFilter<ValidationFilter<CreateWebhookSubscriptionRequest>>()
            .RequireAuthorization(Manage);

        g.MapPost("/webhooks/subscriptions/{subscriptionId}/status", async (string subscriptionId, SetWebhookStatusRequest r, ITenantContext t, IntegrationService svc, CancellationToken ct) =>
            (await svc.SetWebhookStatusAsync(t.RequireTenantId(), subscriptionId, r, t.UserId!, ct)).Match(Results.NoContent))
            .AddEndpointFilter<ValidationFilter<SetWebhookStatusRequest>>()
            .RequireAuthorization(Manage);

        g.MapGet("/events", async (string? status, int? take, ITenantContext t, IntegrationService svc, CancellationToken ct) =>
            Results.Ok(await svc.ListEventsAsync(t.RequireTenantId(), status, take ?? 100, ct)))
            .RequireAuthorization(Read);

        g.MapPost("/events", async (QueueIntegrationEventRequest r, ITenantContext t, IntegrationService svc, CancellationToken ct) =>
            (await svc.QueueEventAsync(t.RequireTenantId(), t.BranchId, r, t.UserId!, ct)).Match(Results.Ok))
            .AddEndpointFilter<ValidationFilter<QueueIntegrationEventRequest>>()
            .RequireAuthorization(Dispatch);

        g.MapGet("/webhooks/deliveries/pending", async (int? take, ITenantContext t, IntegrationService svc, CancellationToken ct) =>
            Results.Ok(await svc.ListPendingDeliveriesAsync(t.RequireTenantId(), take ?? 100, ct)))
            .RequireAuthorization(Dispatch);

        g.MapPost("/webhooks/deliveries/{deliveryId}/success", async (string deliveryId, MarkDeliverySuccessRequest r, ITenantContext t, IntegrationService svc, CancellationToken ct) =>
            (await svc.MarkDeliverySuccessAsync(t.RequireTenantId(), deliveryId, r, ct)).Match(Results.NoContent))
            .AddEndpointFilter<ValidationFilter<MarkDeliverySuccessRequest>>()
            .RequireAuthorization(Dispatch);

        g.MapPost("/webhooks/deliveries/{deliveryId}/failure", async (string deliveryId, MarkDeliveryFailureRequest r, ITenantContext t, IntegrationService svc, CancellationToken ct) =>
            (await svc.MarkDeliveryFailureAsync(t.RequireTenantId(), deliveryId, r, ct)).Match(Results.NoContent))
            .AddEndpointFilter<ValidationFilter<MarkDeliveryFailureRequest>>()
            .RequireAuthorization(Dispatch);

        g.MapGet("/terminals", async (string? branchId, ITenantContext t, IntegrationService svc, CancellationToken ct) =>
            Results.Ok(await svc.ListTerminalsAsync(t.RequireTenantId(), branchId ?? t.BranchId, ct)))
            .RequireAuthorization(Read);

        g.MapPost("/terminals", async (RegisterTerminalRequest r, ITenantContext t, IntegrationService svc, CancellationToken ct) =>
            (await svc.RegisterTerminalAsync(t.RequireTenantId(), t.BranchId, r, t.UserId!, ct))
                .Match(term => Results.Created($"/api/integrations/terminals/{term.Id}", term)))
            .AddEndpointFilter<ValidationFilter<RegisterTerminalRequest>>()
            .RequireAuthorization(Manage);

        g.MapGet("/terminal-commands", async (string? branchId, string? status, int? take, ITenantContext t, IntegrationService svc, CancellationToken ct) =>
            Results.Ok(await svc.ListCommandsAsync(t.RequireTenantId(), branchId ?? t.BranchId, status, take ?? 100, ct)))
            .RequireAuthorization(Read);

        g.MapPost("/terminal-commands", async (QueueTerminalCommandRequest r, ITenantContext t, IntegrationService svc, CancellationToken ct) =>
            (await svc.QueueCommandAsync(t.RequireTenantId(), t.BranchId, r, t.UserId!, ct)).Match(Results.Ok))
            .AddEndpointFilter<ValidationFilter<QueueTerminalCommandRequest>>()
            .RequireAuthorization(Terminal);

        g.MapPost("/terminal-commands/{commandId}/sent", async (string commandId, MarkCommandSentRequest r, ITenantContext t, IntegrationService svc, CancellationToken ct) =>
            (await svc.MarkCommandSentAsync(t.RequireTenantId(), commandId, r, ct)).Match(Results.NoContent))
            .AddEndpointFilter<ValidationFilter<MarkCommandSentRequest>>()
            .RequireAuthorization(Terminal);

        g.MapPost("/terminal-commands/{commandId}/completed", async (string commandId, MarkCommandCompletedRequest r, ITenantContext t, IntegrationService svc, CancellationToken ct) =>
            (await svc.MarkCommandCompletedAsync(t.RequireTenantId(), commandId, r, ct)).Match(Results.NoContent))
            .AddEndpointFilter<ValidationFilter<MarkCommandCompletedRequest>>()
            .RequireAuthorization(Terminal);

        g.MapPost("/terminal-commands/{commandId}/failed", async (string commandId, MarkCommandFailedRequest r, ITenantContext t, IntegrationService svc, CancellationToken ct) =>
            (await svc.MarkCommandFailedAsync(t.RequireTenantId(), commandId, r, ct)).Match(Results.NoContent))
            .AddEndpointFilter<ValidationFilter<MarkCommandFailedRequest>>()
            .RequireAuthorization(Terminal);
    }
}
