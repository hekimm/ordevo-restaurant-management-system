using System.Data;
using Dapper;
using Ordevo.BuildingBlocks.Data;
using Ordevo.Modules.Shift.Application;

namespace Ordevo.Modules.Shift.Infrastructure;

public sealed class ShiftProcedures(IDbConnectionFactory factory) : IShiftProcedures
{
    public async Task<string> OpenSessionAsync(string tenantId, string branchId, string registerId, decimal openingAmount, string userId, CancellationToken ct = default)
    {
        await using var db = await factory.CreateOpenConnectionAsync(ct);
        var p = new DynamicParameters();
        p.Add("p_tenant_id", tenantId);
        p.Add("p_branch_id", branchId);
        p.Add("p_register_id", registerId);
        p.Add("p_opening_amount", openingAmount);
        p.Add("p_user_id", userId);
        p.Add("p_session_id", dbType: DbType.String, direction: ParameterDirection.Output, size: 40);
        await db.ExecuteAsync("PKG_SHIFT.OPEN_SESSION", p, commandType: CommandType.StoredProcedure);
        return p.Get<string>("p_session_id");
    }

    public async Task PayInAsync(string sessionId, decimal amount, string? note, string userId, CancellationToken ct = default)
        => await MoveAsync("PKG_SHIFT.PAY_IN", sessionId, amount, note, userId, ct);

    public async Task PayOutAsync(string sessionId, decimal amount, string? note, string userId, CancellationToken ct = default)
        => await MoveAsync("PKG_SHIFT.PAY_OUT", sessionId, amount, note, userId, ct);

    private async Task MoveAsync(string proc, string sessionId, decimal amount, string? note, string userId, CancellationToken ct)
    {
        await using var db = await factory.CreateOpenConnectionAsync(ct);
        var p = new DynamicParameters();
        p.Add("p_session_id", sessionId);
        p.Add("p_amount", amount);
        p.Add("p_note", note);
        p.Add("p_user_id", userId);
        await db.ExecuteAsync(proc, p, commandType: CommandType.StoredProcedure);
    }

    public async Task<(decimal Expected, decimal Difference)> CloseSessionAsync(string sessionId, decimal counted, string userId, CancellationToken ct = default)
    {
        await using var db = await factory.CreateOpenConnectionAsync(ct);
        var p = new DynamicParameters();
        p.Add("p_session_id", sessionId);
        p.Add("p_counted", counted);
        p.Add("p_user_id", userId);
        p.Add("p_expected", dbType: DbType.Decimal, direction: ParameterDirection.Output);
        p.Add("p_difference", dbType: DbType.Decimal, direction: ParameterDirection.Output);
        await db.ExecuteAsync("PKG_SHIFT.CLOSE_SESSION", p, commandType: CommandType.StoredProcedure);
        return (p.Get<decimal>("p_expected"), p.Get<decimal>("p_difference"));
    }
}
