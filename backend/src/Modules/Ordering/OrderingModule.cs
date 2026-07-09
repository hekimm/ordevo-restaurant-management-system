using FluentValidation;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Ordevo.BuildingBlocks.Abstractions;
using Ordevo.Modules.Ordering.Api;
using Ordevo.Modules.Ordering.Application;
using Ordevo.Modules.Ordering.Infrastructure;
using Ordevo.Modules.Ordering.Realtime;

namespace Ordevo.Modules.Ordering;

public sealed class OrderingModule : IModule
{
    public string Name => "ordering";

    public void RegisterServices(IServiceCollection services, IConfiguration configuration)
    {
        services.AddScoped<ITableRepository, TableRepository>();
        services.AddScoped<IMenuPricing, MenuPricing>();
        services.AddScoped<IOrderReadRepository, OrderReadRepository>();
        services.AddScoped<IOrderingProcedures, OrderingProcedures>();

        services.AddScoped<TableService>();
        services.AddScoped<OrderService>();

        services.AddSignalR();
        services.AddSingleton<IOrderNotifier, SignalROrderNotifier>();

        services.AddValidatorsFromAssemblyContaining<OpenOrderValidator>();
    }

    public void MapEndpoints(IEndpointRouteBuilder endpoints) => OrderingEndpoints.Map(endpoints);
}
