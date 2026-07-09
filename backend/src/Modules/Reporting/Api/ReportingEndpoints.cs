using System.Globalization;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using Ordevo.BuildingBlocks.Multitenancy;
using Ordevo.Modules.Reporting.Application;

namespace Ordevo.Modules.Reporting.Api;

public static class ReportingEndpoints
{
    private const string View = "report.view";

    private static DateTime ParseDate(string? s, DateTime fallback)
        => DateTime.TryParse(s, CultureInfo.InvariantCulture, DateTimeStyles.None, out var d) ? d.Date : fallback;

    private static IResult NoBranch() => Results.Problem(title: "branch.required", detail: "İşlem için şube bağlamı gerekli.", statusCode: 400);

    public static void Map(IEndpointRouteBuilder root)
    {
        var g = root.MapGroup("/api/reporting").WithTags("Reporting").RequireAuthorization(View);
        var today = DateTime.UtcNow.Date;

        g.MapGet("/daily", async (string? date, ITenantContext t, ReportService svc, CancellationToken ct) =>
            t.BranchId is null ? NoBranch() : Results.Ok(await svc.DailyStatsAsync(t.RequireTenantId(), t.BranchId, ParseDate(date, today), ct)));

        g.MapGet("/daily-json", async (string? date, ITenantContext t, ReportService svc, CancellationToken ct) =>
        {
            if (t.BranchId is null) return NoBranch();
            var json = await svc.DailyStatsJsonAsync(t.RequireTenantId(), t.BranchId, ParseDate(date, today), ct);
            return Results.Content(json ?? "{}", "application/json");
        });

        g.MapGet("/top-items", async (string? start, string? end, int? limit, ITenantContext t, ReportService svc, CancellationToken ct) =>
            t.BranchId is null ? NoBranch() : Results.Ok(await svc.TopItemsAsync(t.RequireTenantId(), t.BranchId, ParseDate(start, today.AddDays(-30)), ParseDate(end, today), limit ?? 10, ct)));

        g.MapGet("/hourly", async (string? date, ITenantContext t, ReportService svc, CancellationToken ct) =>
            t.BranchId is null ? NoBranch() : Results.Ok(await svc.HourlyAsync(t.RequireTenantId(), t.BranchId, ParseDate(date, today), ct)));

        g.MapGet("/category-sales", async (string? start, string? end, ITenantContext t, ReportService svc, CancellationToken ct) =>
            t.BranchId is null ? NoBranch() : Results.Ok(await svc.CategorySalesAsync(t.RequireTenantId(), t.BranchId, ParseDate(start, today.AddDays(-30)), ParseDate(end, today), ct)));

        g.MapGet("/payment-methods", async (string? start, string? end, ITenantContext t, ReportService svc, CancellationToken ct) =>
            t.BranchId is null ? NoBranch() : Results.Ok(await svc.PaymentMethodsAsync(t.RequireTenantId(), t.BranchId, ParseDate(start, today.AddDays(-30)), ParseDate(end, today), ct)));

        g.MapGet("/daily-summary", async (string? start, string? end, ITenantContext t, ReportService svc, CancellationToken ct) =>
            t.BranchId is null ? NoBranch() : Results.Ok(await svc.DailySummaryAsync(t.RequireTenantId(), t.BranchId, ParseDate(start, today.AddDays(-30)), ParseDate(end, today), ct)));

        g.MapPost("/archive", async (string? date, ITenantContext t, ReportService svc, CancellationToken ct) =>
        {
            if (t.BranchId is null) return NoBranch();
            await svc.ArchiveDailyAsync(t.RequireTenantId(), t.BranchId, ParseDate(date, today), t.UserId!, ct);
            return Results.NoContent();
        });

        g.MapPost("/refresh-mv", async (ReportService svc, CancellationToken ct) =>
        {
            await svc.RefreshMaterializedViewAsync(ct);
            return Results.NoContent();
        });

        g.MapGet("/ml-export", async (string? start, string? end, ITenantContext t, ReportService svc, CancellationToken ct) =>
        {
            if (t.BranchId is null) return NoBranch();
            var csv = await svc.MlExportCsvAsync(t.RequireTenantId(), t.BranchId, ParseDate(start, today.AddDays(-30)), ParseDate(end, today), ct);
            return Results.File(System.Text.Encoding.UTF8.GetBytes(csv), "text/csv", $"ordevo-export-{today:yyyyMMdd}.csv");
        });
    }
}
