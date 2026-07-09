using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using Ordevo.BuildingBlocks.Http;
using Ordevo.BuildingBlocks.Multitenancy;
using Ordevo.Modules.Menu.Application;

namespace Ordevo.Modules.Menu.Api;

public static class MenuEndpoints
{
    private const string Read = "menu.read";
    private const string Manage = "menu.manage";

    public static void Map(IEndpointRouteBuilder root)
    {
        var g = root.MapGroup("/api/menu").WithTags("Menu");

        g.MapGet("/categories", async (ITenantContext t, MenuService svc, CancellationToken ct) =>
            Results.Ok(await svc.ListCategoriesAsync(t.RequireTenantId(), ct)))
            .RequireAuthorization(Read);

        g.MapPost("/categories", async (UpsertCategoryRequest r, ITenantContext t, MenuService svc, CancellationToken ct) =>
            (await svc.CreateCategoryAsync(t.RequireTenantId(), r, ct)).Match(c => Results.Created($"/api/menu/categories/{c.Id}", c)))
            .AddEndpointFilter<ValidationFilter<UpsertCategoryRequest>>()
            .RequireAuthorization(Manage);

        g.MapPut("/categories/{id}", async (string id, UpsertCategoryRequest r, ITenantContext t, MenuService svc, CancellationToken ct) =>
            (await svc.UpdateCategoryAsync(t.RequireTenantId(), id, r, ct)).Match(Results.Ok))
            .AddEndpointFilter<ValidationFilter<UpsertCategoryRequest>>()
            .RequireAuthorization(Manage);

        g.MapDelete("/categories/{id}", async (string id, ITenantContext t, MenuService svc, CancellationToken ct) =>
            (await svc.DeleteCategoryAsync(t.RequireTenantId(), id, ct)).Match(Results.NoContent))
            .RequireAuthorization(Manage);

        g.MapGet("/items", async (string? categoryId, ITenantContext t, MenuService svc, CancellationToken ct) =>
            Results.Ok(await svc.ListItemsAsync(t.RequireTenantId(), categoryId, ct)))
            .RequireAuthorization(Read);

        g.MapPost("/items", async (UpsertMenuItemRequest r, ITenantContext t, MenuService svc, CancellationToken ct) =>
            (await svc.CreateItemAsync(t.RequireTenantId(), r, ct)).Match(i => Results.Created($"/api/menu/items/{i.Id}", i)))
            .AddEndpointFilter<ValidationFilter<UpsertMenuItemRequest>>()
            .RequireAuthorization(Manage);

        g.MapPut("/items/{id}", async (string id, UpsertMenuItemRequest r, ITenantContext t, MenuService svc, CancellationToken ct) =>
            (await svc.UpdateItemAsync(t.RequireTenantId(), id, r, ct)).Match(Results.Ok))
            .AddEndpointFilter<ValidationFilter<UpsertMenuItemRequest>>()
            .RequireAuthorization(Manage);

        g.MapDelete("/items/{id}", async (string id, ITenantContext t, MenuService svc, CancellationToken ct) =>
            (await svc.DeleteItemAsync(t.RequireTenantId(), id, ct)).Match(Results.NoContent))
            .RequireAuthorization(Manage);

        g.MapPut("/items/{id}/modifier-groups", async (string id, AssignModifierGroupsRequest r, ITenantContext t, MenuService svc, CancellationToken ct) =>
            (await svc.AssignModifierGroupsAsync(t.RequireTenantId(), id, r.GroupIds, ct)).Match(Results.NoContent))
            .AddEndpointFilter<ValidationFilter<AssignModifierGroupsRequest>>()
            .RequireAuthorization(Manage);

        g.MapGet("/modifier-groups", async (ITenantContext t, MenuService svc, CancellationToken ct) =>
            Results.Ok(await svc.ListModifierGroupsAsync(t.RequireTenantId(), ct)))
            .RequireAuthorization(Read);

        g.MapPost("/modifier-groups", async (UpsertModifierGroupRequest r, ITenantContext t, MenuService svc, CancellationToken ct) =>
            (await svc.CreateModifierGroupAsync(t.RequireTenantId(), r, ct)).Match(id => Results.Created($"/api/menu/modifier-groups/{id}", new { id })))
            .AddEndpointFilter<ValidationFilter<UpsertModifierGroupRequest>>()
            .RequireAuthorization(Manage);

        g.MapPut("/modifier-groups/{id}", async (string id, UpsertModifierGroupRequest r, ITenantContext t, MenuService svc, CancellationToken ct) =>
            (await svc.UpdateModifierGroupAsync(t.RequireTenantId(), id, r, ct)).Match(Results.NoContent))
            .AddEndpointFilter<ValidationFilter<UpsertModifierGroupRequest>>()
            .RequireAuthorization(Manage);

        g.MapDelete("/modifier-groups/{id}", async (string id, ITenantContext t, MenuService svc, CancellationToken ct) =>
            (await svc.DeleteModifierGroupAsync(t.RequireTenantId(), id, ct)).Match(Results.NoContent))
            .RequireAuthorization(Manage);

        g.MapPost("/modifier-groups/{id}/modifiers", async (string id, UpsertModifierRequest r, ITenantContext t, MenuService svc, CancellationToken ct) =>
            (await svc.AddModifierAsync(t.RequireTenantId(), id, r, ct)).Match(mid => Results.Created($"/api/menu/modifiers/{mid}", new { id = mid })))
            .AddEndpointFilter<ValidationFilter<UpsertModifierRequest>>()
            .RequireAuthorization(Manage);

        g.MapPut("/modifiers/{id}", async (string id, UpsertModifierRequest r, ITenantContext t, MenuService svc, CancellationToken ct) =>
            (await svc.UpdateModifierAsync(t.RequireTenantId(), id, r, ct)).Match(Results.NoContent))
            .AddEndpointFilter<ValidationFilter<UpsertModifierRequest>>()
            .RequireAuthorization(Manage);

        g.MapDelete("/modifiers/{id}", async (string id, ITenantContext t, MenuService svc, CancellationToken ct) =>
            (await svc.DeleteModifierAsync(t.RequireTenantId(), id, ct)).Match(Results.NoContent))
            .RequireAuthorization(Manage);

        g.MapPost("/items/{id}/barcodes", async (string id, AddBarcodeRequest r, ITenantContext t, MenuService svc, CancellationToken ct) =>
            (await svc.AddBarcodeAsync(t.RequireTenantId(), id, r.Barcode, ct)).Match(Results.NoContent))
            .AddEndpointFilter<ValidationFilter<AddBarcodeRequest>>()
            .RequireAuthorization(Manage);

        g.MapGet("/barcodes/{barcode}", async (string barcode, ITenantContext t, MenuService svc, CancellationToken ct) =>
            (await svc.LookupBarcodeAsync(t.RequireTenantId(), barcode, ct)).Match(Results.Ok))
            .RequireAuthorization(Read);

        g.MapGet("/full", async (bool? activeOnly, ITenantContext t, MenuService svc, CancellationToken ct) =>
            Results.Ok(await svc.GetTreeAsync(t.RequireTenantId(), activeOnly ?? true, ct)))
            .RequireAuthorization(Read);
    }
}
