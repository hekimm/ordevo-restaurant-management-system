using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Ordevo.BuildingBlocks.Abstractions;
using Ordevo.Modules.Shift.Api;
using Ordevo.Modules.Shift.Application;
using Ordevo.Modules.Shift.Infrastructure;

namespace Ordevo.Modules.Shift;

public sealed class ShiftModule : IModule
{
    public string Name => "shift";

    public void RegisterServices(IServiceCollection services, IConfiguration configuration)
    {
        services.AddScoped<IShiftRepository, ShiftRepository>();
        services.AddScoped<IShiftProcedures, ShiftProcedures>();
        services.AddScoped<ShiftService>();
    }

    public void MapEndpoints(IEndpointRouteBuilder endpoints) => ShiftEndpoints.Map(endpoints);
}
