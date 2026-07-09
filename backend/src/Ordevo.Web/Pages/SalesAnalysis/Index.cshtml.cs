using System.Globalization;
using Ordevo.Web.Api;
using Ordevo.Web.Models;

namespace Ordevo.Web.Pages.SalesAnalysis;

public sealed class IndexModel(OrdevoApiClient api) : AppPageModel(api)
{
    public DateTime Start { get; private set; }
    public DateTime End { get; private set; }
    public DailyStatsDto? Daily { get; private set; }
    public IReadOnlyList<DailySummaryDto> DailySummaries { get; private set; } = [];
    public IReadOnlyList<HourlyPointDto> Hourly { get; private set; } = [];
    public IReadOnlyList<PaymentMethodDto> PaymentMethods { get; private set; } = [];
    public IReadOnlyList<TopItemDto> TopItems { get; private set; } = [];
    public IReadOnlyList<CategorySalesDto> CategorySales { get; private set; } = [];

    public decimal PeriodRevenue => DailySummaries.Sum(x => x.Revenue);
    public int PeriodOrders => DailySummaries.Sum(x => x.OrderCount);
    public decimal PeriodAvgTicket => PeriodOrders == 0 ? 0 : PeriodRevenue / PeriodOrders;
    public decimal PeriodDiscount => DailySummaries.Sum(x => x.DiscountTotal);

    public async Task OnGetAsync(string? start, string? end, CancellationToken ct)
    {
        var today = DateTime.Today;
        End = ParseDate(end, today);
        Start = ParseDate(start, End.AddDays(-29));
        if (Start > End)
            (Start, End) = (End, Start);

        var qs = $"start={Start:yyyy-MM-dd}&end={End:yyyy-MM-dd}";
        Daily = await GetOneAsync<DailyStatsDto>($"/api/reporting/daily?date={End:yyyy-MM-dd}", ct);
        DailySummaries = await GetListAsync<DailySummaryDto>($"/api/reporting/daily-summary?{qs}", ct);
        Hourly = await GetListAsync<HourlyPointDto>($"/api/reporting/hourly?date={End:yyyy-MM-dd}", ct);
        PaymentMethods = await GetListAsync<PaymentMethodDto>($"/api/reporting/payment-methods?{qs}", ct);
        TopItems = await GetListAsync<TopItemDto>($"/api/reporting/top-items?{qs}&limit=12", ct);
        CategorySales = await GetListAsync<CategorySalesDto>($"/api/reporting/category-sales?{qs}", ct);
    }

    private static DateTime ParseDate(string? value, DateTime fallback)
        => DateTime.TryParse(value, CultureInfo.InvariantCulture, DateTimeStyles.None, out var date) ? date.Date : fallback.Date;
}
