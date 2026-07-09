using FluentValidation;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Ordevo.BuildingBlocks.Abstractions;
using Ordevo.Modules.Menu.Api;
using Ordevo.Modules.Menu.Application;
using Ordevo.Modules.Menu.Infrastructure;

namespace Ordevo.Modules.Menu;

public sealed class MenuModule : IModule
{
    public string Name => "menu";

    public void RegisterServices(IServiceCollection services, IConfiguration configuration)
    {
        services.AddScoped<ICategoryRepository, CategoryRepository>();
        services.AddScoped<IMenuItemRepository, MenuItemRepository>();
        services.AddScoped<IModifierRepository, ModifierRepository>();
        services.AddScoped<IBarcodeRepository, BarcodeRepository>();

        services.AddScoped<MenuService>();

        services.AddValidatorsFromAssemblyContaining<UpsertCategoryValidator>();
    }

    public void MapEndpoints(IEndpointRouteBuilder endpoints) => MenuEndpoints.Map(endpoints);
}
