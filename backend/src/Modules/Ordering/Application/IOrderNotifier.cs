namespace Ordevo.Modules.Ordering.Application;

public interface IOrderNotifier
{
    Task OrderChangedAsync(string tenantId, string orderId, string action, CancellationToken ct = default);
    Task TablesChangedAsync(string tenantId, CancellationToken ct = default);
}
