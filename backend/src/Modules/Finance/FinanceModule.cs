using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Ordevo.BuildingBlocks.Abstractions;
using Ordevo.Modules.Finance.Api;
using Ordevo.Modules.Finance.Application;
using Ordevo.Modules.Finance.Infrastructure;

namespace Ordevo.Modules.Finance;

public sealed class FinanceModule : IModule
{
    public string Name => "finance";

    public void RegisterServices(IServiceCollection services, IConfiguration configuration)
    {
        services.AddScoped<IFinanceRepository, FinanceRepository>();
        services.AddScoped<FinanceService>();
    }

    public void MapEndpoints(IEndpointRouteBuilder endpoints) => FinanceEndpoints.Map(endpoints);
}
