using System.Globalization;
using System.Text;
using Ordevo.Modules.Reporting.Infrastructure;

namespace Ordevo.Modules.Reporting.Application;

public sealed class ReportService(IReportRepository repo)
{
    public async Task<DailyStatsDto> DailyStatsAsync(string tenantId, string branchId, DateTime date, CancellationToken ct = default)
    {
        var r = await repo.DailyStatsAsync(tenantId, branchId, date, ct);
        return new DailyStatsDto(date.ToString("yyyy-MM-dd"), r.OrderCount, r.Revenue, r.ItemCount, r.AvgTicket);
    }

    public async Task<IReadOnlyList<TopItemDto>> TopItemsAsync(string tenantId, string branchId, DateTime start, DateTime end, int limit, CancellationToken ct = default)
        => (await repo.TopItemsAsync(tenantId, branchId, start, end, limit <= 0 ? 10 : limit, ct))
            .Select(x => new TopItemDto(x.Name ?? "", x.Category, x.Quantity, x.Revenue)).ToList();

    public async Task<IReadOnlyList<HourlyDto>> HourlyAsync(string tenantId, string branchId, DateTime date, CancellationToken ct = default)
        => (await repo.HourlyAsync(tenantId, branchId, date, ct)).Select(x => new HourlyDto(x.Hour, x.OrderCount, x.Revenue)).ToList();

    public async Task<IReadOnlyList<CategorySalesDto>> CategorySalesAsync(string tenantId, string branchId, DateTime start, DateTime end, CancellationToken ct = default)
        => (await repo.CategorySalesAsync(tenantId, branchId, start, end, ct)).Select(x => new CategorySalesDto(x.Category ?? "", x.Quantity, x.Revenue)).ToList();

    public async Task<IReadOnlyList<PaymentMethodDto>> PaymentMethodsAsync(string tenantId, string branchId, DateTime start, DateTime end, CancellationToken ct = default)
        => (await repo.PaymentMethodsAsync(tenantId, branchId, start, end, ct)).Select(x => new PaymentMethodDto(x.Method, x.Amount, x.Cnt)).ToList();

    public async Task<IReadOnlyList<DailySummaryDto>> DailySummaryAsync(string tenantId, string branchId, DateTime start, DateTime end, CancellationToken ct = default)
        => (await repo.DailySummaryAsync(tenantId, branchId, start, end, ct))
            .Select(x => new DailySummaryDto(x.BusinessDate.ToString("yyyy-MM-dd"), x.OrderCount, x.Revenue, x.TaxTotal, x.DiscountTotal)).ToList();

    public Task ArchiveDailyAsync(string tenantId, string branchId, DateTime date, string userId, CancellationToken ct = default)
        => repo.ArchiveDailyAsync(tenantId, branchId, date, userId, ct);

    public Task<string?> DailyStatsJsonAsync(string tenantId, string branchId, DateTime date, CancellationToken ct = default)
        => repo.DailyStatsJsonAsync(tenantId, branchId, date, ct);

    public Task RefreshMaterializedViewAsync(CancellationToken ct = default) => repo.RefreshMaterializedViewAsync(ct);

    public async Task<string> MlExportCsvAsync(string tenantId, string branchId, DateTime start, DateTime end, CancellationToken ct = default)
    {
        var rows = await repo.MlExportAsync(tenantId, branchId, start, end, ct);
        var sb = new StringBuilder();
        sb.AppendLine("date,hour,weekday,item,category,quantity,total");
        foreach (var r in rows)
        {
            sb.Append(r.Dt).Append(',')
              .Append(r.Hr.ToString(CultureInfo.InvariantCulture)).Append(',')
              .Append(Csv(r.Dow)).Append(',')
              .Append(Csv(r.Item)).Append(',')
              .Append(Csv(r.Category)).Append(',')
              .Append(r.Qty.ToString(CultureInfo.InvariantCulture)).Append(',')
              .Append(r.Total.ToString(CultureInfo.InvariantCulture))
              .Append('\n');
        }
        return sb.ToString();
    }

    private static string Csv(string? value)
    {
        value ??= "";
        return value.Contains(',') || value.Contains('"')
            ? "\"" + value.Replace("\"", "\"\"") + "\""
            : value;
    }
}
