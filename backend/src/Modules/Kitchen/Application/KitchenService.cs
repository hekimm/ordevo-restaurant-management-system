using Ordevo.BuildingBlocks.Results;

namespace Ordevo.Modules.Kitchen.Application;

public sealed class KitchenService(IStationRepository stations)
{
    public async Task<IReadOnlyList<StationDto>> ListAsync(string tenantId, string branchId, CancellationToken ct = default)
        => (await stations.ListAsync(tenantId, branchId, ct)).Select(ToDto).ToList();

    public async Task<StationDto> CreateAsync(string tenantId, string branchId, UpsertStationRequest r, CancellationToken ct = default)
    {
        var s = new KdsStation
        {
            Id = Guid.NewGuid().ToString(), TenantId = tenantId, BranchId = branchId,
            Name = r.Name.Trim(), Code = r.Code.Trim(), SortOrder = r.SortOrder, IsActive = r.IsActive
        };
        await stations.InsertAsync(s, ct);
        return ToDto(s);
    }

    public async Task<Result<StationDto>> UpdateAsync(string tenantId, string id, UpsertStationRequest r, CancellationToken ct = default)
    {
        var s = await stations.GetAsync(tenantId, id, ct);
        if (s is null) return Error.NotFound("station.not_found", "İstasyon bulunamadı.");
        s.Name = r.Name.Trim(); s.Code = r.Code.Trim(); s.SortOrder = r.SortOrder; s.IsActive = r.IsActive;
        await stations.UpdateAsync(s, ct);
        return ToDto(s);
    }

    public async Task<Result> DeleteAsync(string tenantId, string id, CancellationToken ct = default)
    {
        var affected = await stations.DeleteAsync(tenantId, id, ct);
        return affected > 0 ? Result.Success() : Error.NotFound("station.not_found", "İstasyon bulunamadı.");
    }

    private static StationDto ToDto(KdsStation s) => new(s.Id, s.Name, s.Code, s.SortOrder, s.IsActive);
}
