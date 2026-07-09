namespace Ordevo.Modules.Shift.Application;

public sealed record RegisterDto(string Id, string Name, bool IsActive);
public sealed record CreateRegisterRequest(string Name);
public sealed record UpdateRegisterRequest(string Name, bool IsActive = true);

public sealed record OpenSessionRequest(string RegisterId, decimal OpeningAmount);
public sealed record CashMoveRequest(decimal Amount, string? Note);
public sealed record CloseSessionRequest(decimal CountedAmount, string? Note);

public sealed class SessionRow
{
    public string Id { get; set; } = default!;
    public string BranchId { get; set; } = default!;
    public string RegisterId { get; set; } = default!;
    public string Status { get; set; } = default!;
    public decimal OpeningAmount { get; set; }
    public DateTimeOffset OpenedAt { get; set; }
    public decimal? ClosingCounted { get; set; }
    public decimal? ClosingExpected { get; set; }
    public decimal? Difference { get; set; }
    public DateTimeOffset? ClosedAt { get; set; }
}

public sealed record SessionDto(
    string Id, string RegisterId, string Status, decimal OpeningAmount, DateTimeOffset OpenedAt,
    decimal? ClosingCounted, decimal? ClosingExpected, decimal? Difference, DateTimeOffset? ClosedAt);

public sealed record CloseSessionResult(string SessionId, decimal Expected, decimal Counted, decimal Difference);

public sealed record PaymentBreakdownDto(string Method, decimal Amount, int Count);

public sealed record ZReportDto(
    string SessionId, string RegisterId, string Status,
    DateTimeOffset OpenedAt, DateTimeOffset? ClosedAt,
    decimal OpeningAmount, decimal ExpectedCash, decimal? CountedCash, decimal? Difference,
    decimal PayInTotal, decimal PayOutTotal,
    int OrderCount, decimal GrossSales,
    IReadOnlyList<PaymentBreakdownDto> PaymentBreakdown);
