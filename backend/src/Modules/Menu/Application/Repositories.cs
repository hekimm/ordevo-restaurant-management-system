using Ordevo.Modules.Menu.Domain;

namespace Ordevo.Modules.Menu.Application;

public interface ICategoryRepository
{
    Task<IReadOnlyList<Category>> ListAsync(string tenantId, CancellationToken ct = default);
    Task<Category?> GetAsync(string tenantId, string id, CancellationToken ct = default);
    Task InsertAsync(Category category, CancellationToken ct = default);
    Task<bool> UpdateAsync(Category category, CancellationToken ct = default);
    Task<int> DeleteAsync(string tenantId, string id, CancellationToken ct = default);
    Task<int> CountItemsAsync(string tenantId, string categoryId, CancellationToken ct = default);
}

public interface IMenuItemRepository
{
    Task<IReadOnlyList<MenuItem>> ListAsync(string tenantId, string? categoryId, CancellationToken ct = default);
    Task<MenuItem?> GetAsync(string tenantId, string id, CancellationToken ct = default);
    Task InsertAsync(MenuItem item, CancellationToken ct = default);
    Task<bool> UpdateAsync(MenuItem item, CancellationToken ct = default);
    Task<int> DeleteAsync(string tenantId, string id, CancellationToken ct = default);

    Task SetModifierGroupsAsync(string itemId, IEnumerable<string> groupIds, CancellationToken ct = default);
    Task<IReadOnlyList<string>> GetModifierGroupIdsAsync(string itemId, CancellationToken ct = default);

    Task<IReadOnlyList<ItemModifierLink>> GetAllModifierLinksAsync(string tenantId, CancellationToken ct = default);
}

public sealed record ItemModifierLink(string ItemId, string GroupId);

public interface IModifierRepository
{
    Task<IReadOnlyList<ModifierGroup>> ListGroupsAsync(string tenantId, CancellationToken ct = default);
    Task<ModifierGroup?> GetGroupAsync(string tenantId, string id, CancellationToken ct = default);
    Task InsertGroupAsync(ModifierGroup group, CancellationToken ct = default);
    Task<bool> UpdateGroupAsync(ModifierGroup group, CancellationToken ct = default);
    Task<int> DeleteGroupAsync(string tenantId, string id, CancellationToken ct = default);

    Task<IReadOnlyList<Modifier>> ListModifiersAsync(string tenantId, CancellationToken ct = default);
    Task InsertModifierAsync(Modifier modifier, CancellationToken ct = default);
    Task<bool> UpdateModifierAsync(Modifier modifier, CancellationToken ct = default);
    Task<int> DeleteModifierAsync(string tenantId, string id, CancellationToken ct = default);
}

public interface IBarcodeRepository
{
    Task AddAsync(string id, string tenantId, string menuItemId, string barcode, CancellationToken ct = default);
    Task<BarcodeLookupDto?> LookupAsync(string tenantId, string barcode, CancellationToken ct = default);
}
