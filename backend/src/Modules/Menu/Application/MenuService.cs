using Ordevo.BuildingBlocks.Results;
using Ordevo.Modules.Menu.Domain;

namespace Ordevo.Modules.Menu.Application;

public sealed class MenuService(
    ICategoryRepository categories,
    IMenuItemRepository items,
    IModifierRepository modifiers,
    IBarcodeRepository barcodes)
{
    public async Task<IReadOnlyList<CategoryDto>> ListCategoriesAsync(string tenantId, CancellationToken ct = default)
        => (await categories.ListAsync(tenantId, ct)).Select(ToDto).ToList();

    public async Task<Result<CategoryDto>> CreateCategoryAsync(string tenantId, UpsertCategoryRequest r, CancellationToken ct = default)
    {
        var c = new Category
        {
            Id = Guid.NewGuid().ToString(), TenantId = tenantId,
            Name = r.Name.Trim(), Color = r.Color, SortOrder = r.SortOrder, IsActive = r.IsActive
        };
        await categories.InsertAsync(c, ct);
        return ToDto(c);
    }

    public async Task<Result<CategoryDto>> UpdateCategoryAsync(string tenantId, string id, UpsertCategoryRequest r, CancellationToken ct = default)
    {
        var existing = await categories.GetAsync(tenantId, id, ct);
        if (existing is null) return Error.NotFound("menu.category_not_found", "Kategori bulunamadı.");

        existing.Name = r.Name.Trim(); existing.Color = r.Color; existing.SortOrder = r.SortOrder; existing.IsActive = r.IsActive;
        await categories.UpdateAsync(existing, ct);
        return ToDto(existing);
    }

    public async Task<Result> DeleteCategoryAsync(string tenantId, string id, CancellationToken ct = default)
    {
        if (await categories.CountItemsAsync(tenantId, id, ct) > 0)
            return Error.Conflict("menu.category_has_items", "Kategoride ürün var; önce ürünleri taşıyın veya silin.");
        var affected = await categories.DeleteAsync(tenantId, id, ct);
        return affected > 0 ? Result.Success() : Error.NotFound("menu.category_not_found", "Kategori bulunamadı.");
    }

    public async Task<IReadOnlyList<MenuItemDto>> ListItemsAsync(string tenantId, string? categoryId, CancellationToken ct = default)
        => (await items.ListAsync(tenantId, categoryId, ct)).Select(ToDto).ToList();

    public async Task<Result<MenuItemDto>> CreateItemAsync(string tenantId, UpsertMenuItemRequest r, CancellationToken ct = default)
    {
        if (await categories.GetAsync(tenantId, r.CategoryId, ct) is null)
            return Error.Validation("menu.invalid_category", "Geçersiz kategori.");

        var i = Map(new MenuItem { Id = Guid.NewGuid().ToString(), TenantId = tenantId }, r);
        await items.InsertAsync(i, ct);
        return ToDto(i);
    }

    public async Task<Result<MenuItemDto>> UpdateItemAsync(string tenantId, string id, UpsertMenuItemRequest r, CancellationToken ct = default)
    {
        var existing = await items.GetAsync(tenantId, id, ct);
        if (existing is null) return Error.NotFound("menu.item_not_found", "Ürün bulunamadı.");
        if (await categories.GetAsync(tenantId, r.CategoryId, ct) is null)
            return Error.Validation("menu.invalid_category", "Geçersiz kategori.");

        await items.UpdateAsync(Map(existing, r), ct);
        return ToDto(existing);
    }

    public async Task<Result> DeleteItemAsync(string tenantId, string id, CancellationToken ct = default)
    {
        var affected = await items.DeleteAsync(tenantId, id, ct);
        return affected > 0 ? Result.Success() : Error.NotFound("menu.item_not_found", "Ürün bulunamadı.");
    }

    public async Task<Result> AssignModifierGroupsAsync(string tenantId, string itemId, string[] groupIds, CancellationToken ct = default)
    {
        if (await items.GetAsync(tenantId, itemId, ct) is null)
            return Error.NotFound("menu.item_not_found", "Ürün bulunamadı.");

        var validGroups = (await modifiers.ListGroupsAsync(tenantId, ct)).Select(g => g.Id).ToHashSet();
        if (groupIds.Any(g => !validGroups.Contains(g)))
            return Error.Validation("menu.invalid_modifier_group", "Geçersiz modifier grubu.");

        await items.SetModifierGroupsAsync(itemId, groupIds, ct);
        return Result.Success();
    }

    public async Task<IReadOnlyList<ModifierGroupDto>> ListModifierGroupsAsync(string tenantId, CancellationToken ct = default)
    {
        var groups = await modifiers.ListGroupsAsync(tenantId, ct);
        var mods = await modifiers.ListModifiersAsync(tenantId, ct);
        var byGroup = mods.GroupBy(m => m.GroupId).ToDictionary(g => g.Key, g => g.ToList());
        return groups.Select(g => new ModifierGroupDto(
            g.Id, g.Name, g.MinSelect, g.MaxSelect, g.IsRequired,
            (byGroup.TryGetValue(g.Id, out var list) ? list : [])
                .Select(m => new ModifierDto(m.Id, m.Name, m.PriceDelta, m.SortOrder, m.IsActive)).ToList()))
            .ToList();
    }

    public async Task<Result<string>> CreateModifierGroupAsync(string tenantId, UpsertModifierGroupRequest r, CancellationToken ct = default)
    {
        var g = new ModifierGroup
        {
            Id = Guid.NewGuid().ToString(), TenantId = tenantId,
            Name = r.Name.Trim(), MinSelect = r.MinSelect, MaxSelect = r.MaxSelect, IsRequired = r.IsRequired
        };
        await modifiers.InsertGroupAsync(g, ct);
        return g.Id;
    }

    public async Task<Result> UpdateModifierGroupAsync(string tenantId, string id, UpsertModifierGroupRequest r, CancellationToken ct = default)
    {
        var g = await modifiers.GetGroupAsync(tenantId, id, ct);
        if (g is null) return Error.NotFound("menu.group_not_found", "Modifier grubu bulunamadı.");
        g.Name = r.Name.Trim(); g.MinSelect = r.MinSelect; g.MaxSelect = r.MaxSelect; g.IsRequired = r.IsRequired;
        await modifiers.UpdateGroupAsync(g, ct);
        return Result.Success();
    }

    public async Task<Result> DeleteModifierGroupAsync(string tenantId, string id, CancellationToken ct = default)
    {
        var affected = await modifiers.DeleteGroupAsync(tenantId, id, ct);
        return affected > 0 ? Result.Success() : Error.NotFound("menu.group_not_found", "Modifier grubu bulunamadı.");
    }

    public async Task<Result<string>> AddModifierAsync(string tenantId, string groupId, UpsertModifierRequest r, CancellationToken ct = default)
    {
        if (await modifiers.GetGroupAsync(tenantId, groupId, ct) is null)
            return Error.NotFound("menu.group_not_found", "Modifier grubu bulunamadı.");
        var m = new Modifier
        {
            Id = Guid.NewGuid().ToString(), TenantId = tenantId, GroupId = groupId,
            Name = r.Name.Trim(), PriceDelta = r.PriceDelta, SortOrder = r.SortOrder, IsActive = r.IsActive
        };
        await modifiers.InsertModifierAsync(m, ct);
        return m.Id;
    }

    public async Task<Result> UpdateModifierAsync(string tenantId, string id, UpsertModifierRequest r, CancellationToken ct = default)
    {
        var existing = (await modifiers.ListModifiersAsync(tenantId, ct)).FirstOrDefault(m => m.Id == id);
        if (existing is null) return Error.NotFound("menu.modifier_not_found", "Modifier bulunamadı.");

        existing.Name = r.Name.Trim();
        existing.PriceDelta = r.PriceDelta;
        existing.SortOrder = r.SortOrder;
        existing.IsActive = r.IsActive;
        await modifiers.UpdateModifierAsync(existing, ct);
        return Result.Success();
    }

    public async Task<Result> DeleteModifierAsync(string tenantId, string id, CancellationToken ct = default)
    {
        var affected = await modifiers.DeleteModifierAsync(tenantId, id, ct);
        return affected > 0 ? Result.Success() : Error.NotFound("menu.modifier_not_found", "Modifier bulunamadı.");
    }

    public async Task<Result> AddBarcodeAsync(string tenantId, string itemId, string barcode, CancellationToken ct = default)
    {
        if (await items.GetAsync(tenantId, itemId, ct) is null)
            return Error.NotFound("menu.item_not_found", "Ürün bulunamadı.");
        await barcodes.AddAsync(Guid.NewGuid().ToString(), tenantId, itemId, barcode.Trim(), ct);
        return Result.Success();
    }

    public async Task<Result<BarcodeLookupDto>> LookupBarcodeAsync(string tenantId, string barcode, CancellationToken ct = default)
    {
        var hit = await barcodes.LookupAsync(tenantId, barcode.Trim(), ct);
        return hit is null ? Error.NotFound("menu.barcode_not_found", "Barkod bulunamadı.") : hit;
    }

    public async Task<MenuTree> GetTreeAsync(string tenantId, bool activeOnly, CancellationToken ct = default)
    {
        var cats = await categories.ListAsync(tenantId, ct);
        var allItems = await items.ListAsync(tenantId, null, ct);
        var links = await items.GetAllModifierLinksAsync(tenantId, ct);
        var groups = await ListModifierGroupsAsync(tenantId, ct);

        if (activeOnly)
        {
            cats = cats.Where(c => c.IsActive).ToList();
            allItems = allItems.Where(i => i.IsActive).ToList();
        }

        var groupsByItem = links.GroupBy(l => l.ItemId)
            .ToDictionary(g => g.Key, g => g.Select(x => x.GroupId).ToList());
        var itemsByCategory = allItems.GroupBy(i => i.CategoryId)
            .ToDictionary(g => g.Key, g => g.ToList());

        var treeCategories = cats.Select(c => new MenuTreeCategory(
            c.Id, c.Name, c.Color, c.SortOrder,
            (itemsByCategory.TryGetValue(c.Id, out var list) ? list : [])
                .Select(i => new MenuTreeItem(
                    i.Id, i.Name, i.Description, i.Price, i.VatRate, i.PrepStation, i.SortOrder,
                    groupsByItem.TryGetValue(i.Id, out var gids) ? gids : []))
                .ToList()))
            .ToList();

        return new MenuTree(treeCategories, groups);
    }

    private static CategoryDto ToDto(Category c) => new(c.Id, c.Name, c.Color, c.SortOrder, c.IsActive);

    private static MenuItemDto ToDto(MenuItem i) => new(
        i.Id, i.CategoryId, i.Name, i.Description, i.Price, i.VatRate, i.Sku, i.ImageUrl, i.PrepStation, i.SortOrder, i.IsActive);

    private static MenuItem Map(MenuItem target, UpsertMenuItemRequest r)
    {
        target.CategoryId = r.CategoryId; target.Name = r.Name.Trim(); target.Description = r.Description;
        target.Price = r.Price; target.VatRate = r.VatRate; target.Sku = r.Sku; target.ImageUrl = r.ImageUrl;
        target.PrepStation = r.PrepStation; target.SortOrder = r.SortOrder; target.IsActive = r.IsActive;
        return target;
    }
}
