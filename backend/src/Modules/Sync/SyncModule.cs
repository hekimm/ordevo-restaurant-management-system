using FluentValidation;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Ordevo.BuildingBlocks.Abstractions;
using Ordevo.Modules.Sync.Api;
using Ordevo.Modules.Sync.Application;
using Ordevo.Modules.Sync.Infrastructure;

namespace Ordevo.Modules.Sync;

public sealed class SyncModule : IModule
{
    public string Name => "sync";

    public void RegisterServices(IServiceCollection services, IConfiguration configuration)
    {
        services.AddScoped<ISyncRepository, SyncRepository>();
        services.AddScoped<ISyncProcedures, SyncProcedures>();
        services.AddScoped<SyncService>();
        services.AddValidatorsFromAssemblyContaining<RegisterDeviceValidator>();
    }

    public void MapEndpoints(IEndpointRouteBuilder endpoints) => SyncEndpoints.Map(endpoints);
}
