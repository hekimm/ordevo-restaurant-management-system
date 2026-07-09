using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Ordevo.BuildingBlocks.Abstractions;
using Ordevo.Modules.Identity.Api;
using Ordevo.Modules.Identity.Application;
using Ordevo.Modules.Identity.Domain;
using Ordevo.Modules.Identity.Infrastructure;

namespace Ordevo.Modules.Identity;

public sealed class IdentityModule : IModule
{
    public string Name => "identity";

    public void RegisterServices(IServiceCollection services, IConfiguration configuration)
    {
        services.Configure<IdentityOptions>(configuration.GetSection(IdentityOptions.SectionName));

        services.AddSingleton<IPasswordHasher, BCryptPasswordHasher>();
        services.AddSingleton<ITokenService, JwtTokenService>();
        services.AddScoped<ITenantRepository, TenantRepository>();
        services.AddScoped<IBranchRepository, BranchRepository>();
        services.AddScoped<IRoleRepository, RoleRepository>();
        services.AddScoped<IUserRepository, UserRepository>();
        services.AddScoped<ISettingsRepository, SettingsRepository>();
        services.AddScoped<IRefreshTokenRepository, RefreshTokenRepository>();
        services.AddScoped<IAuditWriter, AuditWriter>();

        services.AddScoped<AuthService>();
        services.AddScoped<UserService>();
        services.AddScoped<SettingsService>();

        services.AddScoped<IdentitySeeder>();
        services.AddHostedService<IdentitySeederHostedService>();

        services.AddAuthorizationBuilder().AddPermissionPolicies();
    }

    public void MapEndpoints(IEndpointRouteBuilder endpoints)
    {
        AuthEndpoints.Map(endpoints);
        UserEndpoints.Map(endpoints);
        SettingsEndpoints.Map(endpoints);
    }
}

internal static class AuthorizationExtensions
{
    public static AuthorizationBuilder AddPermissionPolicies(this AuthorizationBuilder builder)
    {
        foreach (var code in Permissions.Catalogue.Keys)
        {
            builder.AddPolicy(code, policy =>
                policy.RequireAuthenticatedUser()
                      .RequireClaim("perm", code));
        }
        return builder;
    }
}
