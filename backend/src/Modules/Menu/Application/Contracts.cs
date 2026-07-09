namespace Ordevo.Modules.Menu.Application;

public sealed record CategoryDto(string Id, string Name, string? Color, int SortOrder, bool IsActive);
public sealed record UpsertCategoryRequest(string Name, string? Color, int SortOrder, bool IsActive = true);

public sealed record MenuItemDto(
    string Id, string CategoryId, string Name, string? Description,
    decimal Price, decimal VatRate, string? Sku, string? ImageUrl,
    string? PrepStation, int SortOrder, bool IsActive);

public sealed record UpsertMenuItemRequest(
    string CategoryId, string Name, string? Description, decimal Price, decimal VatRate,
    string? Sku, string? ImageUrl, string? PrepStation, int SortOrder, bool IsActive = true);

public sealed record ModifierDto(string Id, string Name, decimal PriceDelta, int SortOrder, bool IsActive);
public sealed record ModifierGroupDto(string Id, string Name, int MinSelect, int MaxSelect, bool IsRequired, IReadOnlyList<ModifierDto> Modifiers);

public sealed record UpsertModifierGroupRequest(string Name, int MinSelect, int MaxSelect, bool IsRequired);
public sealed record UpsertModifierRequest(string Name, decimal PriceDelta, int SortOrder, bool IsActive = true);
public sealed record AssignModifierGroupsRequest(string[] GroupIds);

public sealed record AddBarcodeRequest(string Barcode);
public sealed record BarcodeLookupDto(string MenuItemId, string Name, decimal Price);

public sealed record MenuTreeItem(
    string Id, string Name, string? Description, decimal Price, decimal VatRate,
    string? PrepStation, int SortOrder, IReadOnlyList<string> ModifierGroupIds);

public sealed record MenuTreeCategory(
    string Id, string Name, string? Color, int SortOrder, IReadOnlyList<MenuTreeItem> Items);

public sealed record MenuTree(
    IReadOnlyList<MenuTreeCategory> Categories,
    IReadOnlyList<ModifierGroupDto> ModifierGroups);
