using System.Text.Json;
using Microsoft.AspNetCore.Mvc;
using Ordevo.Web.Api;
using Ordevo.Web.Models;

namespace Ordevo.Web.Pages.Tables;

public sealed class IndexModel(OrdevoApiClient api) : AppPageModel(api)
{
    public IReadOnlyList<SectionDto> Sections { get; private set; } = [];
    public IReadOnlyList<TableDto> Tables { get; private set; } = [];
    public MenuTree Menu { get; private set; } = new([], []);
    public IReadOnlyList<OrderSummaryDto> OpenOrders { get; private set; } = [];
    public FiscalOverviewDto Fiscal { get; private set; } = new("kapalı", "disabled", false, 0, 0, [], []);

    public async Task OnGetAsync(CancellationToken ct)
    {
        Sections = await GetListAsync<SectionDto>("/api/ordering/sections", ct);
        Tables = await GetListAsync<TableDto>("/api/ordering/tables", ct);
        Menu = await GetOneAsync<MenuTree>("/api/menu/full", ct) ?? new([], []);
        OpenOrders = await GetListAsync<OrderSummaryDto>("/api/ordering/orders?status=open", ct);
        var fiscal = await Api.GetAsync<FiscalOverviewDto>("/api/fiscal/overview", ct);
        if (fiscal.IsSuccess && fiscal.Value is not null)
            Fiscal = fiscal.Value;
    }

    public async Task<IActionResult> OnGetOrderAsync(string id, CancellationToken ct)
        => await ProxyGet<OrderDto>($"/api/ordering/orders/{id}", ct);

    public async Task<IActionResult> OnGetTablesAsync(CancellationToken ct)
        => await ProxyGet<List<TableDto>>("/api/ordering/tables", ct);

    public async Task<IActionResult> OnGetOpenOrdersAsync(CancellationToken ct)
        => await ProxyGet<List<OrderSummaryDto>>("/api/ordering/orders?status=open", ct);

    public async Task<IActionResult> OnGetReceiptAsync(string id, CancellationToken ct)
        => await ProxyGet<ReceiptDocumentDto>($"/api/print/orders/{id}/receipt", ct);

    public async Task<IActionResult> OnPostOpenAsync([FromBody] OpenReq body, CancellationToken ct)
        => await ProxyPost<OrderDto>("/api/ordering/orders",
            new { tableId = body.TableId, orderType = body.OrderType ?? "dinein", guestCount = body.GuestCount ?? 1 }, ct);

    public async Task<IActionResult> OnPostAddItemAsync([FromBody] AddItemReq body, CancellationToken ct)
        => await ProxyPost<OrderDto>($"/api/ordering/orders/{body.OrderId}/items",
            new { menuItemId = body.MenuItemId, quantity = body.Quantity, modifierIds = body.ModifierIds, note = body.Note }, ct);

    public async Task<IActionResult> OnPostSetQtyAsync([FromBody] SetQtyReq body, CancellationToken ct)
        => await ProxyPut<OrderDto>($"/api/ordering/orders/items/{body.ItemId}/quantity",
            new { quantity = body.Quantity }, ct);

    public async Task<IActionResult> OnPostVoidItemAsync([FromBody] VoidItemReq body, CancellationToken ct)
        => await ProxyPost<OrderDto>($"/api/ordering/orders/items/{body.ItemId}/void",
            new { reason = body.Reason }, ct);

    public async Task<IActionResult> OnPostCompItemAsync([FromBody] ItemIdReq body, CancellationToken ct)
        => await ProxyPost<OrderDto>($"/api/ordering/orders/items/{body.ItemId}/comp", new { }, ct);

    public async Task<IActionResult> OnPostItemStatusAsync([FromBody] ItemStatusReq body, CancellationToken ct)
        => await ProxyPut<OrderDto>($"/api/ordering/orders/items/{body.ItemId}/status",
            new { status = body.Status }, ct);

    public async Task<IActionResult> OnPostDiscountAsync([FromBody] DiscountReq body, CancellationToken ct)
        => await ProxyPost<OrderDto>($"/api/ordering/orders/{body.OrderId}/discounts",
            new { type = body.Type, value = body.Value, reason = body.Reason }, ct);

    public async Task<IActionResult> OnPostPayAsync([FromBody] PayReq body, CancellationToken ct)
        => await ProxyPost<FiscalPaymentResultDto>($"/api/fiscal/orders/{body.OrderId}/payments",
            new
            {
                method = body.Method,
                amount = body.Amount,
                tip = body.Tip ?? 0m,
                terminalId = body.TerminalId,
                issueEInvoice = body.IssueEInvoice,
                documentType = body.DocumentType,
                buyerName = body.BuyerName,
                buyerTaxNumber = body.BuyerTaxNumber,
                buyerTaxOffice = body.BuyerTaxOffice,
                buyerAddress = body.BuyerAddress,
                buyerCity = body.BuyerCity,
                buyerEmail = body.BuyerEmail,
                notes = body.Notes
            }, ct);

    public async Task<IActionResult> OnPostCloseAsync([FromBody] OrderIdReq body, CancellationToken ct)
        => await ProxyPost<OrderDto>($"/api/ordering/orders/{body.OrderId}/close", new { }, ct);

    public async Task<IActionResult> OnPostCancelAsync([FromBody] CancelReq body, CancellationToken ct)
        => await ProxyPost<OrderDto>($"/api/ordering/orders/{body.OrderId}/cancel",
            new { reason = body.Reason }, ct);

    public async Task<IActionResult> OnPostTransferAsync([FromBody] TransferReq body, CancellationToken ct)
        => await ProxyPost<OrderDto>($"/api/ordering/orders/{body.OrderId}/transfer",
            new { toTableId = body.ToTableId }, ct);

    public async Task<IActionResult> OnPostMergeAsync([FromBody] MergeReq body, CancellationToken ct)
        => await ProxyPost<OrderDto>($"/api/ordering/orders/{body.OrderId}/merge",
            new { sourceOrderId = body.SourceOrderId }, ct);

    public async Task<IActionResult> OnPostSplitAsync([FromBody] SplitReq body, CancellationToken ct)
        => await ProxyPost<OrderDto>($"/api/ordering/orders/{body.OrderId}/split",
            new { itemIds = body.ItemIds, toTableId = body.ToTableId }, ct);

    public async Task<IActionResult> OnPostQueueReceiptAsync([FromBody] OrderIdReq body, CancellationToken ct)
        => await ProxyPost<JsonElement>($"/api/print/orders/{body.OrderId}/receipt/queue", new { }, ct);

    private async Task<IActionResult> ProxyGet<T>(string path, CancellationToken ct)
    {
        var r = await Api.GetAsync<T>(path, ct);
        return r.IsSuccess ? new JsonResult(r.Value) : Problem(r.Error);
    }

    private async Task<IActionResult> ProxyPost<T>(string path, object body, CancellationToken ct)
    {
        var r = await Api.PostAsync<T>(path, body, ct);
        return r.IsSuccess ? new JsonResult(r.Value) : Problem(r.Error);
    }

    private async Task<IActionResult> ProxyPut<T>(string path, object body, CancellationToken ct)
    {
        var r = await Api.PutAsync<T>(path, body, ct);
        return r.IsSuccess ? new JsonResult(r.Value) : Problem(r.Error);
    }

    private BadRequestObjectResult Problem(string? error) =>
        BadRequest(new { error = FriendlyError.Message(error) });

    public sealed record OpenReq(string? TableId, string? OrderType, int? GuestCount);
    public sealed record AddItemReq(string OrderId, string MenuItemId, decimal Quantity, string[]? ModifierIds, string? Note);
    public sealed record SetQtyReq(string ItemId, decimal Quantity);
    public sealed record ItemIdReq(string ItemId);
    public sealed record VoidItemReq(string ItemId, string? Reason);
    public sealed record ItemStatusReq(string ItemId, string Status);
    public sealed record DiscountReq(string OrderId, string Type, decimal Value, string? Reason);
    public sealed record PayReq(
        string OrderId,
        string Method,
        decimal Amount,
        decimal? Tip,
        string? Reference,
        string? TerminalId,
        bool IssueEInvoice,
        string? DocumentType,
        string? BuyerName,
        string? BuyerTaxNumber,
        string? BuyerTaxOffice,
        string? BuyerAddress,
        string? BuyerCity,
        string? BuyerEmail,
        string? Notes);
    public sealed record OrderIdReq(string OrderId);
    public sealed record CancelReq(string OrderId, string? Reason);
    public sealed record TransferReq(string OrderId, string ToTableId);
    public sealed record MergeReq(string OrderId, string SourceOrderId);
    public sealed record SplitReq(string OrderId, string[] ItemIds, string? ToTableId);
}
