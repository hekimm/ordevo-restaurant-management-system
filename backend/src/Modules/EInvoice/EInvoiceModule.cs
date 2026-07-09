using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Ordevo.BuildingBlocks.Abstractions;
using Ordevo.Modules.EInvoice.Api;
using Ordevo.Modules.EInvoice.Application;
using Ordevo.Modules.EInvoice.Infrastructure;

namespace Ordevo.Modules.EInvoice;

public sealed class EInvoiceModule : IModule
{
    public string Name => "einvoice";

    public void RegisterServices(IServiceCollection services, IConfiguration configuration)
    {
        services.AddHttpClient();
        services.AddScoped<IEInvoiceRepository, EInvoiceRepository>();
        services.AddScoped<EInvoiceService>();

        services.AddScoped<MockEInvoiceProvider>();
        services.AddScoped<SpecialIntegratorEInvoiceProvider>();
        var selected = configuration["EInvoice:Provider"] ?? "mock";
        services.AddScoped<IEInvoiceProvider>(sp => selected.ToLowerInvariant() switch
        {
            "special_integrator" or "private_integrator" or "ozel_entegrator" => sp.GetRequiredService<SpecialIntegratorEInvoiceProvider>(),
            _ => sp.GetRequiredService<MockEInvoiceProvider>(),
        });
    }

    public void MapEndpoints(IEndpointRouteBuilder endpoints) => EInvoiceEndpoints.Map(endpoints);
}
