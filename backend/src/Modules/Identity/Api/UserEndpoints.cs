using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using Ordevo.BuildingBlocks.Http;
using Ordevo.BuildingBlocks.Multitenancy;
using Ordevo.Modules.Identity.Application;
using Ordevo.Modules.Identity.Domain;

namespace Ordevo.Modules.Identity.Api;

public static class UserEndpoints
{
    public static void Map(IEndpointRouteBuilder root)
    {
        var group = root.MapGroup("/api/identity/users").WithTags("Identity.Users");

        group.MapGet("/", async (ITenantContext tenant, UserService users, CancellationToken ct) =>
        {
            var list = await users.ListAsync(tenant.RequireTenantId(), ct);
            return Results.Ok(list);
        }).RequireAuthorization(Permissions.UsersRead);

        group.MapPost("/", async (CreateUserRequest request, ITenantContext tenant, UserService users, CancellationToken ct) =>
        {
            var result = await users.CreateAsync(tenant.RequireTenantId(), tenant.UserId!, request, ct);
            return result.Match(summary => Results.Created($"/api/identity/users/{summary.Id}", summary));
        }).RequireAuthorization(Permissions.UsersWrite);

        group.MapPost("/waiters", async (CreateWaiterRequest request, ITenantContext tenant, UserService users, CancellationToken ct) =>
        {
            var result = await users.CreateWaiterAsync(tenant.RequireTenantId(), tenant.UserId!, request, ct);
            return result.Match(summary => Results.Created($"/api/identity/users/{summary.Id}", summary));
        }).RequireAuthorization(Permissions.UsersWrite);

        group.MapPut("/{id}", async (string id, UpdateUserRequest request, ITenantContext tenant, UserService users, CancellationToken ct) =>
        {
            var result = await users.UpdateAsync(tenant.RequireTenantId(), tenant.UserId!, id, request, ct);
            return result.Match(Results.Ok);
        }).RequireAuthorization(Permissions.UsersWrite);

        group.MapPost("/{id}/pin", async (string id, ResetUserPinRequest request, ITenantContext tenant, UserService users, CancellationToken ct) =>
        {
            var result = await users.ResetPinAsync(tenant.RequireTenantId(), tenant.UserId!, id, request, ct);
            return result.Match(Results.NoContent);
        }).RequireAuthorization(Permissions.UsersWrite);

        group.MapDelete("/{id}", async (string id, ITenantContext tenant, UserService users, CancellationToken ct) =>
        {
            var result = await users.DeactivateAsync(tenant.RequireTenantId(), tenant.UserId!, id, ct);
            return result.Match(Results.NoContent);
        }).RequireAuthorization(Permissions.UsersWrite);

        group.MapPost("/me/change-password", async (ChangePasswordRequest request, ITenantContext tenant, UserService users, CancellationToken ct) =>
        {
            if (tenant.UserId is null) return Results.Unauthorized();
            var result = await users.ChangePasswordAsync(tenant.UserId, request, ct);
            return result.Match(Results.NoContent);
        }).RequireAuthorization();
    }
}
