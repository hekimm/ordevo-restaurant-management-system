using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Ordevo.BuildingBlocks.Abstractions;
using Ordevo.Modules.Print.Api;
using Ordevo.Modules.Print.Application;
using Ordevo.Modules.Print.Infrastructure;

namespace Ordevo.Modules.Print;

public sealed class PrintModule : IModule
{
    public string Name => "print";

    public void RegisterServices(IServiceCollection services, IConfiguration configuration)
    {
        services.AddScoped<IPrintRepository, PrintRepository>();
        services.AddScoped<PrintService>();
    }

    public void MapEndpoints(IEndpointRouteBuilder endpoints) => PrintEndpoints.Map(endpoints);
}
