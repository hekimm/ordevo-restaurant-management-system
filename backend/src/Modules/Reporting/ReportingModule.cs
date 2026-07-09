using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Ordevo.BuildingBlocks.Abstractions;
using Ordevo.Modules.Reporting.Api;
using Ordevo.Modules.Reporting.Application;
using Ordevo.Modules.Reporting.Infrastructure;

namespace Ordevo.Modules.Reporting;

public sealed class ReportingModule : IModule
{
    public string Name => "reporting";

    public void RegisterServices(IServiceCollection services, IConfiguration configuration)
    {
        services.AddScoped<IReportRepository, ReportRepository>();
        services.AddScoped<ReportService>();
    }

    public void MapEndpoints(IEndpointRouteBuilder endpoints) => ReportingEndpoints.Map(endpoints);
}
