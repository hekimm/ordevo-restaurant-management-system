using FluentValidation;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Ordevo.BuildingBlocks.Abstractions;
using Ordevo.Modules.Payment.Api;
using Ordevo.Modules.Payment.Application;
using Ordevo.Modules.Payment.Infrastructure;

namespace Ordevo.Modules.Payment;

public sealed class PaymentModule : IModule
{
    public string Name => "payment";

    public void RegisterServices(IServiceCollection services, IConfiguration configuration)
    {
        services.AddScoped<IPaymentReadRepository, PaymentReadRepository>();
        services.AddScoped<IPaymentProcedures, PaymentProcedures>();
        services.AddScoped<PaymentService>();
        services.AddValidatorsFromAssemblyContaining<AddPaymentValidator>();
    }

    public void MapEndpoints(IEndpointRouteBuilder endpoints) => PaymentEndpoints.Map(endpoints);
}
