using Ordevo.Web.Api;
using Ordevo.Web.Models;

namespace Ordevo.Web.Pages;

public sealed class DashboardModel(OrdevoApiClient api) : AppPageModel(api)
{
    public DailyStatsDto? Daily { get; private set; }
    public IReadOnlyList<TableDto> Tables { get; private set; } = [];
    public IReadOnlyList<SectionDto> Sections { get; private set; } = [];
    public IReadOnlyList<OrderSummaryDto> OpenOrders { get; private set; } = [];
    public IReadOnlyList<KdsTicketDto> KitchenTickets { get; private set; } = [];

    public IReadOnlyDictionary<string, OrderSummaryDto> OpenByTable { get; private set; } =
        new Dictionary<string, OrderSummaryDto>();

    public decimal OpenTotal { get; private set; }
    public int KitchenItemCount { get; private set; }

    public async Task OnGetAsync(CancellationToken ct)
    {
        Daily = await GetOneAsync<DailyStatsDto>("/api/reporting/daily", ct);
        Tables = await GetListAsync<TableDto>("/api/ordering/tables", ct);
        Sections = await GetListAsync<SectionDto>("/api/ordering/sections", ct);
        OpenOrders = await GetListAsync<OrderSummaryDto>("/api/ordering/orders?status=open", ct);
        KitchenTickets = await GetListAsync<KdsTicketDto>("/api/kitchen/board", ct);
        OpenByTable = OpenOrders
            .Where(o => !string.IsNullOrWhiteSpace(o.TableId) && o.ItemCount > 0)
            .GroupBy(o => o.TableId!)
            .ToDictionary(g => g.Key, g => g.OrderByDescending(o => o.OpenedAt).First());

        OpenTotal = OpenOrders.Sum(o => o.Total);
        KitchenItemCount = KitchenTickets.Sum(t => t.Items.Count);
    }

    public static string Elapsed(DateTimeOffset since)
    {
        var mins = (int)Math.Max(0, (DateTimeOffset.Now - since.ToLocalTime()).TotalMinutes);
        return mins < 60 ? $"{mins} dk" : $"{mins / 60}s {mins % 60}dk";
    }

    public static string AreaIcon(string name)
    {
        var n = name.ToLowerInvariant();
        if (n.Contains("bah") || n.Contains("garden")) return "bi-tree";
        if (n.Contains("teras") || n.Contains("terrace")) return "bi-sun";
        if (n.Contains("bar")) return "bi-cup-straw";
        if (n.Contains("vip") || n.Contains("local")) return "bi-star";
        return "bi-door-open";
    }

    public static string AreaKind(string name)
    {
        var n = name.ToLowerInvariant();
        if (n.Contains("bah") || n.Contains("garden")) return "garden";
        if (n.Contains("teras") || n.Contains("terrace")) return "terrace";
        if (n.Contains("bar")) return "bar";
        if (n.Contains("vip") || n.Contains("local")) return "vip";
        return "salon";
    }

    public static string RowLabel(int index)
        => index < 26 ? ((char)('A' + index)).ToString() : $"R{index + 1}";
}
