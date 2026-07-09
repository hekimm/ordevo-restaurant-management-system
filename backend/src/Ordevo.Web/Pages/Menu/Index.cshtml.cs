using Microsoft.AspNetCore.Mvc;
using Ordevo.Web.Api;
using Ordevo.Web.Models;

namespace Ordevo.Web.Pages.Menu;

public sealed class IndexModel(OrdevoApiClient api) : AppPageModel(api)
{
    public MenuTree? Tree { get; private set; }
    public IReadOnlyList<CategoryDto> Categories { get; private set; } = [];
    public IReadOnlyList<MenuItemDto> Items { get; private set; } = [];
    public IReadOnlyList<ModifierGroupDto> ModifierGroups { get; private set; } = [];
    public int ItemCount => Tree?.Categories.Sum(c => c.Items.Count) ?? 0;

    [BindProperty]
    public CategoryInput Category { get; set; } = new();

    [BindProperty]
    public ItemInput Item { get; set; } = new();

    [BindProperty]
    public ModifierGroupInput ModifierGroup { get; set; } = new();

    [BindProperty]
    public ModifierInput Modifier { get; set; } = new();

    public async Task OnGetAsync(CancellationToken ct)
    {
        await LoadAsync(ct);
    }

    public async Task<IActionResult> OnPostCategoryAsync(CancellationToken ct)
    {
        var body = new { Category.Name, Category.Color, Category.SortOrder, Category.IsActive };
        var result = string.IsNullOrWhiteSpace(Category.Id)
            ? await Api.PostAsync<CategoryDto>("/api/menu/categories", body, ct)
            : await Api.PutAsync<CategoryDto>($"/api/menu/categories/{Category.Id}", body, ct);

        return await CompleteMutationAsync(result, ct);
    }

    public async Task<IActionResult> OnPostDeleteCategoryAsync(string id, CancellationToken ct)
    {
        var result = await Api.DeleteAsync<string>($"/api/menu/categories/{id}", ct);
        return await CompleteMutationAsync(result, ct);
    }

    public async Task<IActionResult> OnPostItemAsync(CancellationToken ct)
    {
        var body = new
        {
            Item.CategoryId,
            Item.Name,
            Item.Description,
            Item.Price,
            Item.VatRate,
            Item.Sku,
            Item.ImageUrl,
            Item.PrepStation,
            Item.SortOrder,
            Item.IsActive
        };

        var result = string.IsNullOrWhiteSpace(Item.Id)
            ? await Api.PostAsync<MenuItemDto>("/api/menu/items", body, ct)
            : await Api.PutAsync<MenuItemDto>($"/api/menu/items/{Item.Id}", body, ct);

        return await CompleteMutationAsync(result, ct);
    }

    public async Task<IActionResult> OnPostDeleteItemAsync(string id, CancellationToken ct)
    {
        var result = await Api.DeleteAsync<string>($"/api/menu/items/{id}", ct);
        return await CompleteMutationAsync(result, ct);
    }

    public async Task<IActionResult> OnPostModifierGroupAsync(CancellationToken ct)
    {
        var body = new
        {
            ModifierGroup.Name,
            ModifierGroup.MinSelect,
            ModifierGroup.MaxSelect,
            ModifierGroup.IsRequired
        };

        var result = string.IsNullOrWhiteSpace(ModifierGroup.Id)
            ? await Api.PostAsync<object>("/api/menu/modifier-groups", body, ct)
            : await Api.PutAsync<object>($"/api/menu/modifier-groups/{ModifierGroup.Id}", body, ct);

        return await CompleteMutationAsync(result, ct);
    }

    public async Task<IActionResult> OnPostDeleteModifierGroupAsync(string id, CancellationToken ct)
    {
        var result = await Api.DeleteAsync<string>($"/api/menu/modifier-groups/{id}", ct);
        return await CompleteMutationAsync(result, ct);
    }

    public async Task<IActionResult> OnPostModifierAsync(CancellationToken ct)
    {
        var body = new
        {
            Modifier.Name,
            Modifier.PriceDelta,
            Modifier.SortOrder,
            Modifier.IsActive
        };

        var result = string.IsNullOrWhiteSpace(Modifier.Id)
            ? await Api.PostAsync<object>($"/api/menu/modifier-groups/{Modifier.GroupId}/modifiers", body, ct)
            : await Api.PutAsync<object>($"/api/menu/modifiers/{Modifier.Id}", body, ct);

        return await CompleteMutationAsync(result, ct);
    }

    public async Task<IActionResult> OnPostAssignModifierGroupsAsync(string id, string[] groupIds, CancellationToken ct)
    {
        var result = await Api.PutAsync<string>($"/api/menu/items/{id}/modifier-groups", new { groupIds }, ct);
        return await CompleteMutationAsync(result, ct);
    }

    public async Task<IActionResult> OnPostDeleteModifierAsync(string id, CancellationToken ct)
    {
        var result = await Api.DeleteAsync<string>($"/api/menu/modifiers/{id}", ct);
        return await CompleteMutationAsync(result, ct);
    }

    private async Task LoadAsync(CancellationToken ct)
    {
        Categories = await GetListAsync<CategoryDto>("/api/menu/categories", ct);
        Items = await GetListAsync<MenuItemDto>("/api/menu/items", ct);
        ModifierGroups = await GetListAsync<ModifierGroupDto>("/api/menu/modifier-groups", ct);
        Tree = await GetOneAsync<MenuTree>("/api/menu/full?activeOnly=false", ct);

        Category.IsActive = true;
        Item.IsActive = true;
        Item.VatRate = Item.VatRate == 0 ? 10 : Item.VatRate;
        Item.CategoryId = string.IsNullOrWhiteSpace(Item.CategoryId) ? Categories.FirstOrDefault()?.Id ?? "" : Item.CategoryId;
        ModifierGroup.MaxSelect = ModifierGroup.MaxSelect == 0 ? 1 : ModifierGroup.MaxSelect;
        Modifier.IsActive = true;
    }

    private async Task<IActionResult> CompleteMutationAsync<T>(ApiResult<T> result, CancellationToken ct)
    {
        if (result.IsSuccess)
        {
            NotifySuccess("Menü güncellendi.");
            return RedirectToPage();
        }

        Errors.Add(UiFormat.Error(result));
        await LoadAsync(ct);
        return Page();
    }

    public sealed class CategoryInput
    {
        public string? Id { get; set; }
        public string Name { get; set; } = "";
        public string? Color { get; set; }
        public int SortOrder { get; set; }
        public bool IsActive { get; set; } = true;
    }

    public sealed class ItemInput
    {
        public string? Id { get; set; }
        public string CategoryId { get; set; } = "";
        public string Name { get; set; } = "";
        public string? Description { get; set; }
        public decimal Price { get; set; }
        public decimal VatRate { get; set; } = 10;
        public string? Sku { get; set; }
        public string? ImageUrl { get; set; }
        public string? PrepStation { get; set; }
        public int SortOrder { get; set; }
        public bool IsActive { get; set; } = true;
    }

    public sealed class ModifierGroupInput
    {
        public string? Id { get; set; }
        public string Name { get; set; } = "";
        public int MinSelect { get; set; }
        public int MaxSelect { get; set; } = 1;
        public bool IsRequired { get; set; }
    }

    public sealed class ModifierInput
    {
        public string? Id { get; set; }
        public string GroupId { get; set; } = "";
        public string Name { get; set; } = "";
        public decimal PriceDelta { get; set; }
        public int SortOrder { get; set; }
        public bool IsActive { get; set; } = true;
    }
}
