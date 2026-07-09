namespace Ordevo.Modules.Reporting.Application;


public sealed class DailyStatsRow
{
    public int OrderCount { get; set; }
    public decimal Revenue { get; set; }
    public decimal ItemCount { get; set; }
    public decimal AvgTicket { get; set; }
}
public sealed record DailyStatsDto(string Date, int OrderCount, decimal Revenue, decimal ItemCount, decimal AvgTicket);

public sealed class TopItemRow
{
    public string? Name { get; set; }
    public string? Category { get; set; }
    public decimal Quantity { get; set; }
    public decimal Revenue { get; set; }
}
public sealed record TopItemDto(string Name, string? Category, decimal Quantity, decimal Revenue);

public sealed class HourlyRow { public int Hour { get; set; } public int OrderCount { get; set; } public decimal Revenue { get; set; } }
public sealed record HourlyDto(int Hour, int OrderCount, decimal Revenue);

public sealed class CategorySalesRow { public string? Category { get; set; } public decimal Quantity { get; set; } public decimal Revenue { get; set; } }
public sealed record CategorySalesDto(string Category, decimal Quantity, decimal Revenue);

public sealed class PaymentMethodRow { public string Method { get; set; } = default!; public decimal Amount { get; set; } public int Cnt { get; set; } }
public sealed record PaymentMethodDto(string Method, decimal Amount, int Count);

public sealed class DailySummaryRow
{
    public DateTimeOffset BusinessDate { get; set; }
    public int OrderCount { get; set; }
    public decimal Revenue { get; set; }
    public decimal TaxTotal { get; set; }
    public decimal DiscountTotal { get; set; }
}
public sealed record DailySummaryDto(string BusinessDate, int OrderCount, decimal Revenue, decimal TaxTotal, decimal DiscountTotal);

public sealed class MlExportRow
{
    public string? Dt { get; set; }
    public int Hr { get; set; }
    public string? Dow { get; set; }
    public string? Item { get; set; }
    public string? Category { get; set; }
    public decimal Qty { get; set; }
    public decimal Total { get; set; }
}
