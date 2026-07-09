namespace Ordevo.Modules.Shift.Application;

public interface IShiftRepository
{
    Task<IReadOnlyList<RegisterDto>> ListRegistersAsync(string tenantId, string branchId, CancellationToken ct = default);
    Task InsertRegisterAsync(string id, string tenantId, string branchId, string name, CancellationToken ct = default);
    Task<bool> UpdateRegisterAsync(string tenantId, string branchId, string id, UpdateRegisterRequest request, CancellationToken ct = default);
    Task<int> DeleteRegisterAsync(string tenantId, string branchId, string id, CancellationToken ct = default);

    Task<SessionRow?> GetSessionAsync(string tenantId, string sessionId, CancellationToken ct = default);
    Task<SessionRow?> GetOpenSessionForRegisterAsync(string tenantId, string registerId, CancellationToken ct = default);

    Task<ZReportDto?> GetZReportAsync(string tenantId, string sessionId, CancellationToken ct = default);
}

public interface IShiftProcedures
{
    Task<string> OpenSessionAsync(string tenantId, string branchId, string registerId, decimal openingAmount, string userId, CancellationToken ct = default);
    Task PayInAsync(string sessionId, decimal amount, string? note, string userId, CancellationToken ct = default);
    Task PayOutAsync(string sessionId, decimal amount, string? note, string userId, CancellationToken ct = default);
    Task<(decimal Expected, decimal Difference)> CloseSessionAsync(string sessionId, decimal counted, string userId, CancellationToken ct = default);
}
