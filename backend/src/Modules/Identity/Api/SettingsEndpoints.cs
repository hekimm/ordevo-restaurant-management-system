using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using Ordevo.BuildingBlocks.Multitenancy;
using Ordevo.Modules.Identity.Application;
using Ordevo.Modules.Identity.Domain;

namespace Ordevo.Modules.Identity.Api;

public static class SettingsEndpoints
{
    public static void Map(IEndpointRouteBuilder root)
    {
        var group = root.MapGroup("/api/settings").WithTags("Settings");

        group.MapGet("/runtime", async (ITenantContext tenant, SettingsService settings, CancellationToken ct) =>
            Results.Ok(await settings.GetDeveloperSettingsAsync(tenant.RequireTenantId(), tenant.BranchId, ct)))
            .RequireAuthorization();

        group.MapGet("/developer", async (ITenantContext tenant, SettingsService settings, CancellationToken ct) =>
            Results.Ok(await settings.GetDeveloperSettingsAsync(tenant.RequireTenantId(), tenant.BranchId, ct)))
            .RequireAuthorization(Permissions.SettingsManage);

        group.MapPut("/developer", async (
            UpdateDeveloperSettingsRequest request,
            ITenantContext tenant,
            SettingsService settings,
            CancellationToken ct) =>
        {
            var result = await settings.UpdateDeveloperSettingsAsync(
                tenant.RequireTenantId(),
                tenant.BranchId,
                request,
                tenant.UserId!,
                ct);

            return Results.Ok(result);
        }).RequireAuthorization(Permissions.SettingsManage);
    }
}

