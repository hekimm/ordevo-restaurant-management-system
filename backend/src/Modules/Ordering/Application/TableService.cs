using Ordevo.BuildingBlocks.Results;
using Ordevo.Modules.Ordering.Domain;

namespace Ordevo.Modules.Ordering.Application;

public sealed class TableService(ITableRepository tables, IOrderNotifier notifier)
{
    public async Task<IReadOnlyList<SectionDto>> ListSectionsAsync(string tenantId, string branchId, CancellationToken ct = default)
        => (await tables.ListSectionsAsync(tenantId, branchId, ct)).Select(s => new SectionDto(s.Id, s.Name, s.SortOrder)).ToList();

    public async Task<SectionDto> CreateSectionAsync(string tenantId, string branchId, UpsertSectionRequest r, CancellationToken ct = default)
    {
        var s = new TableSection { Id = Guid.NewGuid().ToString(), TenantId = tenantId, BranchId = branchId, Name = r.Name.Trim(), SortOrder = r.SortOrder };
        await tables.InsertSectionAsync(s, ct);
        return new SectionDto(s.Id, s.Name, s.SortOrder);
    }

    public async Task<Result<SectionDto>> UpdateSectionAsync(string tenantId, string id, UpsertSectionRequest r, CancellationToken ct = default)
    {
        var existing = await tables.GetSectionAsync(tenantId, id, ct);
        if (existing is null) return Error.NotFound("section.not_found", "Bölüm bulunamadı.");

        existing.Name = r.Name.Trim();
        existing.SortOrder = r.SortOrder;
        await tables.UpdateSectionAsync(existing, ct);
        await notifier.TablesChangedAsync(tenantId, ct);
        return new SectionDto(existing.Id, existing.Name, existing.SortOrder);
    }

    public async Task<Result> DeleteSectionAsync(string tenantId, string id, CancellationToken ct = default)
    {
        var affected = await tables.DeleteSectionAsync(tenantId, id, ct);
        if (affected > 0) await notifier.TablesChangedAsync(tenantId, ct);
        return affected > 0 ? Result.Success() : Error.NotFound("section.not_found", "Bölüm bulunamadı.");
    }

    public async Task<IReadOnlyList<TableDto>> ListTablesAsync(string tenantId, string branchId, CancellationToken ct = default)
        => (await tables.ListTablesAsync(tenantId, branchId, ct))
            .Select(t => new TableDto(t.Id, t.SectionId, t.Name, t.Capacity, t.Status, t.SortOrder, t.IsActive)).ToList();

    public async Task<TableDto> CreateTableAsync(string tenantId, string branchId, UpsertTableRequest r, CancellationToken ct = default)
    {
        var t = new DiningTable
        {
            Id = Guid.NewGuid().ToString(), TenantId = tenantId, BranchId = branchId,
            SectionId = r.SectionId, Name = r.Name.Trim(), Capacity = r.Capacity, SortOrder = r.SortOrder, IsActive = r.IsActive
        };
        await tables.InsertTableAsync(t, ct);
        await notifier.TablesChangedAsync(tenantId, ct);
        return new TableDto(t.Id, t.SectionId, t.Name, t.Capacity, "idle", t.SortOrder, t.IsActive);
    }

    public async Task<Result<TableDto>> UpdateTableAsync(string tenantId, string id, UpsertTableRequest r, CancellationToken ct = default)
    {
        var existing = await tables.GetTableAsync(tenantId, id, ct);
        if (existing is null) return Error.NotFound("table.not_found", "Masa bulunamadı.");

        existing.SectionId = r.SectionId; existing.Name = r.Name.Trim(); existing.Capacity = r.Capacity;
        existing.SortOrder = r.SortOrder; existing.IsActive = r.IsActive;
        await tables.UpdateTableAsync(existing, ct);
        await notifier.TablesChangedAsync(tenantId, ct);
        return new TableDto(existing.Id, existing.SectionId, existing.Name, existing.Capacity, existing.Status, existing.SortOrder, existing.IsActive);
    }

    public async Task<Result> DeleteTableAsync(string tenantId, string id, CancellationToken ct = default)
    {
        var affected = await tables.DeleteTableAsync(tenantId, id, ct);
        if (affected > 0) await notifier.TablesChangedAsync(tenantId, ct);
        return affected > 0 ? Result.Success() : Error.NotFound("table.not_found", "Masa bulunamadı.");
    }
}
