using FluentValidation;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Ordevo.BuildingBlocks.Abstractions;
using Ordevo.Modules.Inventory.Api;
using Ordevo.Modules.Inventory.Application;
using Ordevo.Modules.Inventory.Infrastructure;

namespace Ordevo.Modules.Inventory;

public sealed class InventoryModule : IModule
{
    public string Name => "inventory";

    public void RegisterServices(IServiceCollection services, IConfiguration configuration)
    {
        services.AddScoped<IInventoryRepository, InventoryRepository>();
        services.AddScoped<IInventoryProcedures, InventoryProcedures>();
        services.AddScoped<InventoryService>();
        services.AddValidatorsFromAssemblyContaining<UpsertStockItemValidator>();
    }

    public void MapEndpoints(IEndpointRouteBuilder endpoints) => InventoryEndpoints.Map(endpoints);
}
