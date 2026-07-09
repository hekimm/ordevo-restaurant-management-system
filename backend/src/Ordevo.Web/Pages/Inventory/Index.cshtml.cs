using Microsoft.AspNetCore.Mvc;
using Ordevo.Web.Api;
using Ordevo.Web.Models;

namespace Ordevo.Web.Pages.Inventory;

public sealed class IndexModel(OrdevoApiClient api) : AppPageModel(api)
{
    public IReadOnlyList<StockItemDto> StockItems { get; private set; } = [];
    public IReadOnlyList<UnitDto> Units { get; private set; } = [];
    public IReadOnlyList<SupplierDto> Suppliers { get; private set; } = [];

    [BindProperty]
    public StockInput Stock { get; set; } = new();

    [BindProperty]
    public UnitInput Unit { get; set; } = new();

    [BindProperty]
    public SupplierInput Supplier { get; set; } = new();

    [BindProperty]
    public AdjustInput Adjust { get; set; } = new();

    [BindProperty]
    public PurchaseInput Purchase { get; set; } = new();

    public async Task OnGetAsync(CancellationToken ct)
    {
        await LoadAsync(ct);
    }

    public async Task<IActionResult> OnPostUnitAsync(CancellationToken ct)
    {
        var body = new { Unit.Code, Unit.Name };
        var result = string.IsNullOrWhiteSpace(Unit.Id)
            ? await Api.PostAsync<UnitDto>("/api/inventory/units", body, ct)
            : await Api.PutAsync<UnitDto>($"/api/inventory/units/{Unit.Id}", body, ct);
        return await CompleteMutationAsync(result, ct);
    }

    public async Task<IActionResult> OnPostDeleteUnitAsync(string id, CancellationToken ct)
    {
        var result = await Api.DeleteAsync<string>($"/api/inventory/units/{id}", ct);
        return await CompleteMutationAsync(result, ct);
    }

    public async Task<IActionResult> OnPostSupplierAsync(CancellationToken ct)
    {
        var body = new
        {
            Supplier.Name,
            Supplier.Phone,
            Supplier.Email,
            Supplier.TaxNo,
            Supplier.IsActive
        };

        var result = string.IsNullOrWhiteSpace(Supplier.Id)
            ? await Api.PostAsync<SupplierDto>("/api/inventory/suppliers", body, ct)
            : await Api.PutAsync<SupplierDto>($"/api/inventory/suppliers/{Supplier.Id}", body, ct);

        return await CompleteMutationAsync(result, ct);
    }

    public async Task<IActionResult> OnPostDeleteSupplierAsync(string id, CancellationToken ct)
    {
        var result = await Api.DeleteAsync<string>($"/api/inventory/suppliers/{id}", ct);
        return await CompleteMutationAsync(result, ct);
    }

    public async Task<IActionResult> OnPostStockAsync(CancellationToken ct)
    {
        var body = new
        {
            Stock.Name,
            Stock.Sku,
            Stock.UnitId,
            Stock.ReorderLevel,
            Stock.UnitCost,
            Stock.IsActive
        };

        var result = string.IsNullOrWhiteSpace(Stock.Id)
            ? await Api.PostAsync<StockItemDto>("/api/inventory/stock-items", body, ct)
            : await Api.PutAsync<StockItemDto>($"/api/inventory/stock-items/{Stock.Id}", body, ct);

        return await CompleteMutationAsync(result, ct);
    }

    public async Task<IActionResult> OnPostDeleteStockAsync(string id, CancellationToken ct)
    {
        var result = await Api.DeleteAsync<string>($"/api/inventory/stock-items/{id}", ct);
        return await CompleteMutationAsync(result, ct);
    }

    public async Task<IActionResult> OnPostAdjustAsync(CancellationToken ct)
    {
        var result = await Api.PostAsync<StockItemDto>($"/api/inventory/stock-items/{Adjust.StockItemId}/adjust", new
        {
            Adjust.NewQuantity,
            Adjust.Reason
        }, ct);

        return await CompleteMutationAsync(result, ct);
    }

    public async Task<IActionResult> OnPostPurchaseAsync(CancellationToken ct)
    {
        var result = await Api.PostAsync<PurchaseDto>("/api/inventory/purchases", new
        {
            SupplierId = string.IsNullOrWhiteSpace(Purchase.SupplierId) ? null : Purchase.SupplierId,
            Purchase.Note,
            Lines = new[]
            {
                new { Purchase.StockItemId, Purchase.Quantity, Purchase.UnitCost }
            }
        }, ct);

        return await CompleteMutationAsync(result, ct);
    }

    private async Task LoadAsync(CancellationToken ct)
    {
        StockItems = await GetListAsync<StockItemDto>("/api/inventory/stock-items", ct);
        Units = await GetListAsync<UnitDto>("/api/inventory/units", ct);
        Suppliers = await GetListAsync<SupplierDto>("/api/inventory/suppliers", ct);

        Stock.UnitId = string.IsNullOrWhiteSpace(Stock.UnitId) ? Units.FirstOrDefault()?.Id ?? "" : Stock.UnitId;
        Stock.IsActive = true;
        Supplier.IsActive = true;
        Adjust.StockItemId = string.IsNullOrWhiteSpace(Adjust.StockItemId) ? StockItems.FirstOrDefault()?.Id ?? "" : Adjust.StockItemId;
        Purchase.StockItemId = string.IsNullOrWhiteSpace(Purchase.StockItemId) ? StockItems.FirstOrDefault()?.Id ?? "" : Purchase.StockItemId;
        Purchase.Quantity = Purchase.Quantity == 0 ? 1 : Purchase.Quantity;
    }

    private async Task<IActionResult> CompleteMutationAsync<T>(ApiResult<T> result, CancellationToken ct)
    {
        if (result.IsSuccess)
        {
            NotifySuccess("Stok işlemi tamamlandı.");
            return RedirectToPage();
        }

        Errors.Add(UiFormat.Error(result));
        await LoadAsync(ct);
        return Page();
    }

    public sealed class StockInput
    {
        public string? Id { get; set; }
        public string Name { get; set; } = "";
        public string? Sku { get; set; }
        public string UnitId { get; set; } = "";
        public decimal ReorderLevel { get; set; }
        public decimal UnitCost { get; set; }
        public bool IsActive { get; set; } = true;
    }

    public sealed class UnitInput
    {
        public string Code { get; set; } = "";
        public string Name { get; set; } = "";
        public string? Id { get; set; }
    }

    public sealed class SupplierInput
    {
        public string? Id { get; set; }
        public string Name { get; set; } = "";
        public string? Phone { get; set; }
        public string? Email { get; set; }
        public string? TaxNo { get; set; }
        public bool IsActive { get; set; } = true;
    }

    public sealed class AdjustInput
    {
        public string StockItemId { get; set; } = "";
        public decimal NewQuantity { get; set; }
        public string? Reason { get; set; }
    }

    public sealed class PurchaseInput
    {
        public string? SupplierId { get; set; }
        public string StockItemId { get; set; } = "";
        public decimal Quantity { get; set; } = 1;
        public decimal UnitCost { get; set; }
        public string? Note { get; set; }
    }
}
