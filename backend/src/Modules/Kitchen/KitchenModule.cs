using FluentValidation;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Ordevo.BuildingBlocks.Abstractions;
using Ordevo.Modules.Kitchen.Api;
using Ordevo.Modules.Kitchen.Application;
using Ordevo.Modules.Kitchen.Infrastructure;
using Ordevo.Modules.Kitchen.Realtime;

namespace Ordevo.Modules.Kitchen;

public sealed class KitchenModule : IModule
{
    public string Name => "kitchen";

    public void RegisterServices(IServiceCollection services, IConfiguration configuration)
    {
        services.AddScoped<IStationRepository, StationRepository>();
        services.AddScoped<IKdsRepository, KdsRepository>();
        services.AddScoped<KitchenService>();
        services.AddScoped<KdsService>();
        services.AddSingleton<IKitchenNotifier, KdsNotifier>();
        services.AddValidatorsFromAssemblyContaining<UpsertStationValidator>();
    }

    public void MapEndpoints(IEndpointRouteBuilder endpoints) => KitchenEndpoints.Map(endpoints);
}
