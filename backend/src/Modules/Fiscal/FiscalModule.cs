using FluentValidation;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Ordevo.BuildingBlocks.Abstractions;
using Ordevo.Modules.Fiscal.Api;
using Ordevo.Modules.Fiscal.Application;
using Ordevo.Modules.Fiscal.Infrastructure;

namespace Ordevo.Modules.Fiscal;

public sealed class FiscalModule : IModule
{
    public string Name => "fiscal";

    public void RegisterServices(IServiceCollection services, IConfiguration configuration)
    {
        services.AddHttpClient();
        services.AddValidatorsFromAssemblyContaining<FiscalPaymentValidator>();
        services.AddScoped<IFiscalTransactionRepository, FiscalTransactionRepository>();
        services.AddScoped<FiscalService>();

        services.AddScoped<SandboxPaymentTerminalProvider>();
        services.AddScoped<HttpPaymentTerminalProvider>();
        services.AddScoped<NullEAdisyonProvider>();
        services.AddScoped<HttpEAdisyonProvider>();

        services.AddScoped<IPaymentTerminalProvider>(sp =>
        {
            var selected = configuration["Fiscal:PaymentTerminal:Provider"] ?? "sandbox";
            return selected.Trim().ToLowerInvariant() switch
            {
                "http" or "gmp3-agent" or "gmp3_http" => sp.GetRequiredService<HttpPaymentTerminalProvider>(),
                _ => sp.GetRequiredService<SandboxPaymentTerminalProvider>()
            };
        });

        services.AddScoped<IEAdisyonProvider>(sp =>
        {
            var enabled = configuration.GetValue("Fiscal:EAdisyon:Enabled", false);
            if (!enabled) return sp.GetRequiredService<NullEAdisyonProvider>();

            var selected = configuration["Fiscal:EAdisyon:Provider"] ?? "http";
            return selected.Trim().ToLowerInvariant() switch
            {
                "http" or "special_integrator" or "ozel_entegrator" => sp.GetRequiredService<HttpEAdisyonProvider>(),
                _ => sp.GetRequiredService<NullEAdisyonProvider>()
            };
        });
    }

    public void MapEndpoints(IEndpointRouteBuilder endpoints) => FiscalEndpoints.Map(endpoints);
}
