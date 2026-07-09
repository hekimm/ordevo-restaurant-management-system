using System.Text;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.IdentityModel.Tokens;
using Ordevo.BuildingBlocks.Auth;
using Ordevo.BuildingBlocks.Data;
using Ordevo.BuildingBlocks.Multitenancy;

namespace Ordevo.BuildingBlocks;

public static class BuildingBlocksExtensions
{
    public static IServiceCollection AddBuildingBlocks(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        DapperOracleSetup.Apply();

        services.AddHttpContextAccessor();
        services.AddScoped<ITenantContext, TenantContext>();

        var connectionString = configuration.GetConnectionString("Oracle")
            ?? throw new InvalidOperationException("ConnectionStrings:Oracle is not configured.");
        services.AddSingleton<IDbConnectionFactory>(new OracleConnectionFactory(connectionString));

        services.Configure<JwtOptions>(configuration.GetSection(JwtOptions.SectionName));
        var jwt = configuration.GetSection(JwtOptions.SectionName).Get<JwtOptions>() ?? new JwtOptions();

        services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
            .AddJwtBearer(options =>
            {
                options.TokenValidationParameters = new TokenValidationParameters
                {
                    ValidateIssuer = true,
                    ValidateAudience = true,
                    ValidateLifetime = true,
                    ValidateIssuerSigningKey = true,
                    ValidIssuer = jwt.Issuer,
                    ValidAudience = jwt.Audience,
                    IssuerSigningKey = string.IsNullOrEmpty(jwt.SigningKey)
                        ? new SymmetricSecurityKey(new byte[32])
                        : new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwt.SigningKey)),
                    ClockSkew = TimeSpan.FromSeconds(30)
                };

                options.Events = new JwtBearerEvents
                {
                    OnMessageReceived = context =>
                    {
                        var accessToken = context.Request.Query["access_token"];
                        var path = context.HttpContext.Request.Path;
                        if (!string.IsNullOrEmpty(accessToken) && path.StartsWithSegments("/hubs"))
                            context.Token = accessToken;
                        return Task.CompletedTask;
                    }
                };
            });

        services.AddAuthorization();
        return services;
    }
}
