using FluentValidation;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Ordevo.BuildingBlocks.Abstractions;
using Ordevo.Modules.Integration.Api;
using Ordevo.Modules.Integration.Application;
using Ordevo.Modules.Integration.Infrastructure;

namespace Ordevo.Modules.Integration;

public sealed class IntegrationModule : IModule
{
    public string Name => "integration";

    public void RegisterServices(IServiceCollection services, IConfiguration configuration)
    {
        services.AddValidatorsFromAssemblyContaining<CreateConnectorValidator>();
        services.AddScoped<IIntegrationRepository, IntegrationRepository>();
        services.AddScoped<IIntegrationProcedures, IntegrationProcedures>();
        services.AddScoped<IntegrationService>();
    }

    public void MapEndpoints(IEndpointRouteBuilder endpoints) => IntegrationEndpoints.Map(endpoints);
}
