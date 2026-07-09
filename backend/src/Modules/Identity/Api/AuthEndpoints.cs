using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using Ordevo.BuildingBlocks.Http;
using Ordevo.BuildingBlocks.Multitenancy;
using Ordevo.Modules.Identity.Application;

namespace Ordevo.Modules.Identity.Api;

public static class AuthEndpoints
{
    public static void Map(IEndpointRouteBuilder root)
    {
        var group = root.MapGroup("/api/identity/auth").WithTags("Identity.Auth");

        group.MapPost("/login", async (LoginRequest request, AuthService auth, HttpContext http, CancellationToken ct) =>
        {
            var result = await auth.LoginAsync(request, ClientIp(http), ct);
            return result.Match(Results.Ok);
        }).AllowAnonymous();

        group.MapPost("/refresh", async (RefreshRequest request, AuthService auth, HttpContext http, CancellationToken ct) =>
        {
            var result = await auth.RefreshAsync(request.RefreshToken, ClientIp(http), ct);
            return result.Match(Results.Ok);
        }).AllowAnonymous();

        group.MapPost("/logout", async (LogoutRequest request, AuthService auth, CancellationToken ct) =>
        {
            await auth.LogoutAsync(request.RefreshToken, ct);
            return Results.NoContent();
        }).AllowAnonymous();

        group.MapGet("/me", async (ITenantContext tenantContext, IUserRepository users, ITenantRepository tenants, CancellationToken ct) =>
        {
            if (!tenantContext.IsAuthenticated || tenantContext.UserId is null)
                return Results.Unauthorized();

            var user = await users.GetByIdAsync(tenantContext.UserId, ct);
            var tenant = user is null ? null : await tenants.GetByIdAsync(user.TenantId, ct);
            if (user is null || tenant is null)
                return Results.Unauthorized();

            var roles = await users.GetRoleNamesAsync(user.Id, ct);
            var permissions = await users.GetPermissionCodesAsync(user.Id, ct);
            var branchIds = await users.GetBranchIdsAsync(user.Id, ct);

            return Results.Ok(new UserProfile(
                user.Id, tenant.Id, tenant.Slug, user.Email, user.FullName,
                [.. roles], [.. permissions], [.. branchIds]));
        }).RequireAuthorization();
    }

    private static string? ClientIp(HttpContext http) => http.Connection.RemoteIpAddress?.ToString();
}
