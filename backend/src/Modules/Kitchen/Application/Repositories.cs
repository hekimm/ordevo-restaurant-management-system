namespace Ordevo.Modules.Kitchen.Application;

public interface IStationRepository
{
    Task<IReadOnlyList<KdsStation>> ListAsync(string tenantId, string branchId, CancellationToken ct = default);
    Task<KdsStation?> GetAsync(string tenantId, string id, CancellationToken ct = default);
    Task InsertAsync(KdsStation station, CancellationToken ct = default);
    Task<bool> UpdateAsync(KdsStation station, CancellationToken ct = default);
    Task<int> DeleteAsync(string tenantId, string id, CancellationToken ct = default);
}

public interface IKdsRepository
{
    Task<IReadOnlyList<KdsItemRow>> GetBoardAsync(string tenantId, string branchId, string? stationCode, CancellationToken ct = default);

    Task<DateTimeOffset> GetDatabaseNowAsync(CancellationToken ct = default);

    Task<KdsItemState?> GetItemStateAsync(string tenantId, string itemId, CancellationToken ct = default);

    Task<IReadOnlyList<string>> GetActiveItemIdsAsync(string tenantId, string orderId, CancellationToken ct = default);
}

public sealed record KdsItemState(string OrderId, string ItemStatus, string OrderStatus);
