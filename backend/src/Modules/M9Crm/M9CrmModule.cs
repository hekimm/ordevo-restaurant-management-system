using FluentValidation;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Ordevo.BuildingBlocks.Abstractions;
using Ordevo.Modules.M9Crm.Api;
using Ordevo.Modules.M9Crm.Application;
using Ordevo.Modules.M9Crm.Infrastructure;

namespace Ordevo.Modules.M9Crm;

public sealed class M9CrmModule : IModule
{
    public string Name => "m9-crm";

    public void RegisterServices(IServiceCollection services, IConfiguration configuration)
    {
        services.AddScoped<IM9CrmRepository, M9CrmRepository>();
        services.AddScoped<IM9CrmProcedures, M9CrmProcedures>();
        services.AddScoped<M9CrmService>();
        services.AddValidatorsFromAssemblyContaining<CreateCustomerValidator>();
    }

    public void MapEndpoints(IEndpointRouteBuilder endpoints) => M9CrmEndpoints.Map(endpoints);
}
