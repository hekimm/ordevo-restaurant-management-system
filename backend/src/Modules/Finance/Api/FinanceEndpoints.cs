using System.Globalization;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using Ordevo.BuildingBlocks.Http;
using Ordevo.BuildingBlocks.Multitenancy;
using Ordevo.Modules.Finance.Application;

namespace Ordevo.Modules.Finance.Api;

public static class FinanceEndpoints
{
    private const string Read = "finance.read";
    private const string Manage = "finance.manage";

    private static DateTime ParseDate(string? value, DateTime fallback)
        => DateTime.TryParse(value, CultureInfo.InvariantCulture, DateTimeStyles.None, out var date) ? date.Date : fallback.Date;

    private static IResult NoBranch() => Results.Problem(title: "branch.required", detail: "İşlem için şube bağlamı gerekli.", statusCode: 400);

    public static void Map(IEndpointRouteBuilder root)
    {
        var g = root.MapGroup("/api/finance").WithTags("Finance");
        var today = DateTime.UtcNow.Date;

        g.MapGet("/accounts", async (ITenantContext t, FinanceService svc, CancellationToken ct) =>
            Results.Ok(await svc.ListAccountsAsync(t.RequireTenantId(), t.BranchId, ct))).RequireAuthorization(Read);

        g.MapPost("/accounts", async (CreateFinanceAccountRequest r, ITenantContext t, FinanceService svc, CancellationToken ct) =>
            (await svc.CreateAccountAsync(t.RequireTenantId(), t.BranchId, r, ct)).Match(x => Results.Created($"/api/finance/accounts/{x.Id}", x)))
            .RequireAuthorization(Manage);
        g.MapPut("/accounts/{id}", async (string id, UpdateFinanceAccountRequest r, ITenantContext t, FinanceService svc, CancellationToken ct) =>
            (await svc.UpdateAccountAsync(t.RequireTenantId(), id, r, ct)).Match(Results.Ok))
            .RequireAuthorization(Manage);
        g.MapDelete("/accounts/{id}", async (string id, ITenantContext t, FinanceService svc, CancellationToken ct) =>
            (await svc.DeactivateAccountAsync(t.RequireTenantId(), id, ct)).Match(Results.NoContent))
            .RequireAuthorization(Manage);

        g.MapGet("/counterparties", async (string? type, ITenantContext t, FinanceService svc, CancellationToken ct) =>
            Results.Ok(await svc.ListCounterpartiesAsync(t.RequireTenantId(), type, ct))).RequireAuthorization(Read);

        g.MapPost("/counterparties", async (CreateCounterpartyRequest r, ITenantContext t, FinanceService svc, CancellationToken ct) =>
            (await svc.CreateCounterpartyAsync(t.RequireTenantId(), r, ct)).Match(x => Results.Created($"/api/finance/counterparties/{x.Id}", x)))
            .RequireAuthorization(Manage);
        g.MapPut("/counterparties/{id}", async (string id, UpdateCounterpartyRequest r, ITenantContext t, FinanceService svc, CancellationToken ct) =>
            (await svc.UpdateCounterpartyAsync(t.RequireTenantId(), id, r, ct)).Match(Results.Ok))
            .RequireAuthorization(Manage);
        g.MapDelete("/counterparties/{id}", async (string id, ITenantContext t, FinanceService svc, CancellationToken ct) =>
            (await svc.DeactivateCounterpartyAsync(t.RequireTenantId(), id, ct)).Match(Results.NoContent))
            .RequireAuthorization(Manage);

        g.MapGet("/transactions", async (string? start, string? end, string? type, ITenantContext t, FinanceService svc, CancellationToken ct) =>
            t.BranchId is null ? NoBranch() : Results.Ok(await svc.ListTransactionsAsync(t.RequireTenantId(), t.BranchId, ParseDate(start, today.AddDays(-30)), ParseDate(end, today), type, ct)))
            .RequireAuthorization(Read);

        g.MapPost("/transactions", async (CreateFinanceTransactionRequest r, ITenantContext t, FinanceService svc, CancellationToken ct) =>
            t.BranchId is null ? NoBranch()
                : (await svc.CreateTransactionAsync(t.RequireTenantId(), t.BranchId, t.UserId!, r, ct)).Match(x => Results.Created($"/api/finance/transactions/{x.Id}", x)))
            .RequireAuthorization(Manage);
        g.MapPut("/transactions/{id}", async (string id, CreateFinanceTransactionRequest r, ITenantContext t, FinanceService svc, CancellationToken ct) =>
            t.BranchId is null ? NoBranch()
                : (await svc.UpdateTransactionAsync(t.RequireTenantId(), t.BranchId, id, r, ct)).Match(Results.Ok))
            .RequireAuthorization(Manage);
        g.MapDelete("/transactions/{id}", async (string id, ITenantContext t, FinanceService svc, CancellationToken ct) =>
            t.BranchId is null ? NoBranch()
                : (await svc.VoidTransactionAsync(t.RequireTenantId(), t.BranchId, id, ct)).Match(Results.NoContent))
            .RequireAuthorization(Manage);

        g.MapGet("/summary", async (string? start, string? end, ITenantContext t, FinanceService svc, CancellationToken ct) =>
            t.BranchId is null ? NoBranch() : Results.Ok(await svc.SummaryAsync(t.RequireTenantId(), t.BranchId, ParseDate(start, today.AddDays(-30)), ParseDate(end, today), ct)))
            .RequireAuthorization(Read);

        g.MapGet("/cashflow", async (string? start, string? end, ITenantContext t, FinanceService svc, CancellationToken ct) =>
            t.BranchId is null ? NoBranch() : Results.Ok(await svc.CashflowAsync(t.RequireTenantId(), t.BranchId, ParseDate(start, today.AddDays(-30)), ParseDate(end, today), ct)))
            .RequireAuthorization(Read);

        g.MapGet("/profit-loss", async (string? start, string? end, ITenantContext t, FinanceService svc, CancellationToken ct) =>
            t.BranchId is null ? NoBranch() : Results.Ok(await svc.ProfitLossAsync(t.RequireTenantId(), t.BranchId, ParseDate(start, today.AddDays(-30)), ParseDate(end, today), ct)))
            .RequireAuthorization(Read);
    }
}
