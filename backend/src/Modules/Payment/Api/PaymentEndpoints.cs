using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using Ordevo.BuildingBlocks.Http;
using Ordevo.BuildingBlocks.Multitenancy;
using Ordevo.Modules.Payment.Application;

namespace Ordevo.Modules.Payment.Api;

public static class PaymentEndpoints
{
    private const string Process = "payment.process";
    private const string Refund = "payment.refund";
    private const string Read = "order.read";

    public static void Map(IEndpointRouteBuilder root)
    {
        var g = root.MapGroup("/api/payment").WithTags("Payment");

        g.MapPost("/orders/{orderId}/payments", async (string orderId, AddPaymentRequest r, ITenantContext t, PaymentService svc, CancellationToken ct) =>
            (await svc.AddPaymentAsync(t.RequireTenantId(), orderId, t.UserId!, r, ct)).Match(Results.Ok))
            .AddEndpointFilter<ValidationFilter<AddPaymentRequest>>().RequireAuthorization(Process);

        g.MapGet("/orders/{orderId}/payments", async (string orderId, ITenantContext t, PaymentService svc, CancellationToken ct) =>
            (await svc.GetPaymentsAsync(t.RequireTenantId(), orderId, ct)).Match(Results.Ok))
            .RequireAuthorization(Process);

        g.MapPost("/payments/{paymentId}/void", async (string paymentId, ITenantContext t, PaymentService svc, CancellationToken ct) =>
            (await svc.VoidPaymentAsync(t.RequireTenantId(), paymentId, t.UserId!, ct)).Match(Results.Ok))
            .RequireAuthorization(Process);

        g.MapPost("/orders/{orderId}/refunds", async (string orderId, RefundRequest r, ITenantContext t, PaymentService svc, CancellationToken ct) =>
            (await svc.RefundAsync(t.RequireTenantId(), orderId, t.UserId!, r, ct)).Match(id => Results.Ok(new { refundId = id })))
            .AddEndpointFilter<ValidationFilter<RefundRequest>>().RequireAuthorization(Refund);

        g.MapGet("/orders/{orderId}/invoice", async (string orderId, ITenantContext t, PaymentService svc, CancellationToken ct) =>
            (await svc.GetInvoiceAsync(t.RequireTenantId(), orderId, ct)).Match(Results.Ok))
            .RequireAuthorization(Read);
    }
}
