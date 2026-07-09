using Oracle.ManagedDataAccess.Client;
using Ordevo.BuildingBlocks.Results;

namespace Ordevo.Modules.Shift.Application;

public sealed class ShiftService(IShiftRepository repo, IShiftProcedures procs)
{
    public Task<IReadOnlyList<RegisterDto>> ListRegistersAsync(string tenantId, string branchId, CancellationToken ct = default)
        => repo.ListRegistersAsync(tenantId, branchId, ct);

    public async Task<Result<RegisterDto>> CreateRegisterAsync(string tenantId, string branchId, CreateRegisterRequest r, CancellationToken ct = default)
    {
        var name = r.Name.Trim();
        if (string.IsNullOrWhiteSpace(name))
            return Error.Validation("register.name", "Kasa adı zorunlu.");

        var id = Guid.NewGuid().ToString();
        await repo.InsertRegisterAsync(id, tenantId, branchId, name, ct);
        return new RegisterDto(id, name, true);
    }

    public async Task<Result<RegisterDto>> UpdateRegisterAsync(string tenantId, string branchId, string id, UpdateRegisterRequest r, CancellationToken ct = default)
    {
        var name = r.Name.Trim();
        if (string.IsNullOrWhiteSpace(name))
            return Error.Validation("register.name", "Kasa adı zorunlu.");

        if (!r.IsActive && await repo.GetOpenSessionForRegisterAsync(tenantId, id, ct) is not null)
            return Error.Conflict("register.open_session", "Açık oturumu olan kasa pasifleştirilemez.");

        if (!await repo.UpdateRegisterAsync(tenantId, branchId, id, new UpdateRegisterRequest(name, r.IsActive), ct))
            return Error.NotFound("register.not_found", "Kasa bulunamadı.");

        var register = (await repo.ListRegistersAsync(tenantId, branchId, ct)).First(x => x.Id == id);
        return register;
    }

    public async Task<Result> DeleteRegisterAsync(string tenantId, string branchId, string id, CancellationToken ct = default)
    {
        if (await repo.GetOpenSessionForRegisterAsync(tenantId, id, ct) is not null)
            return Error.Conflict("register.open_session", "Açık oturumu olan kasa pasifleştirilemez.");

        var affected = await repo.DeleteRegisterAsync(tenantId, branchId, id, ct);
        return affected > 0 ? Result.Success() : Error.NotFound("register.not_found", "Kasa bulunamadı.");
    }

    public async Task<Result<SessionDto>> OpenSessionAsync(string tenantId, string branchId, string userId, OpenSessionRequest r, CancellationToken ct = default)
    {
        try
        {
            var id = await procs.OpenSessionAsync(tenantId, branchId, r.RegisterId, r.OpeningAmount, userId, ct);
            return ToDto((await repo.GetSessionAsync(tenantId, id, ct))!);
        }
        catch (OracleException ex) when (TryBusiness(ex, out var error)) { return error; }
    }

    public async Task<Result<SessionDto>> GetSessionAsync(string tenantId, string sessionId, CancellationToken ct = default)
    {
        var s = await repo.GetSessionAsync(tenantId, sessionId, ct);
        return s is null ? Error.NotFound("session.not_found", "Kasa oturumu bulunamadı.") : ToDto(s);
    }

    public async Task<Result<SessionDto>> GetOpenSessionAsync(string tenantId, string registerId, CancellationToken ct = default)
    {
        var s = await repo.GetOpenSessionForRegisterAsync(tenantId, registerId, ct);
        return s is null ? Error.NotFound("session.none_open", "Açık kasa oturumu yok.") : ToDto(s);
    }

    public Task<Result<SessionDto>> PayInAsync(string tenantId, string sessionId, CashMoveRequest r, string userId, CancellationToken ct = default)
        => MoveAsync(tenantId, sessionId, () => procs.PayInAsync(sessionId, r.Amount, r.Note, userId, ct), ct);

    public Task<Result<SessionDto>> PayOutAsync(string tenantId, string sessionId, CashMoveRequest r, string userId, CancellationToken ct = default)
        => MoveAsync(tenantId, sessionId, () => procs.PayOutAsync(sessionId, r.Amount, r.Note, userId, ct), ct);

    public async Task<Result<CloseSessionResult>> CloseSessionAsync(string tenantId, string sessionId, CloseSessionRequest r, string userId, CancellationToken ct = default)
    {
        if (await repo.GetSessionAsync(tenantId, sessionId, ct) is null)
            return Error.NotFound("session.not_found", "Kasa oturumu bulunamadı.");
        try
        {
            var (expected, difference) = await procs.CloseSessionAsync(sessionId, r.CountedAmount, userId, ct);
            return new CloseSessionResult(sessionId, expected, r.CountedAmount, difference);
        }
        catch (OracleException ex) when (TryBusiness(ex, out var error)) { return error; }
    }

    public async Task<Result<ZReportDto>> GetZReportAsync(string tenantId, string sessionId, CancellationToken ct = default)
    {
        var z = await repo.GetZReportAsync(tenantId, sessionId, ct);
        return z is null ? Error.NotFound("session.not_found", "Kasa oturumu bulunamadı.") : z;
    }

    private async Task<Result<SessionDto>> MoveAsync(string tenantId, string sessionId, Func<Task> op, CancellationToken ct)
    {
        if (await repo.GetSessionAsync(tenantId, sessionId, ct) is null)
            return Error.NotFound("session.not_found", "Kasa oturumu bulunamadı.");
        try
        {
            await op();
            return ToDto((await repo.GetSessionAsync(tenantId, sessionId, ct))!);
        }
        catch (OracleException ex) when (TryBusiness(ex, out var error)) { return error; }
    }

    private static SessionDto ToDto(SessionRow s) => new(
        s.Id, s.RegisterId, s.Status, s.OpeningAmount, s.OpenedAt,
        s.ClosingCounted, s.ClosingExpected, s.Difference, s.ClosedAt);

    private static bool TryBusiness(OracleException ex, out Error error)
    {
        if (ex.Number is >= 20301 and <= 20310)
        {
            var message = ex.Message.Split('\n')[0].Replace($"ORA-{ex.Number}:", "").Trim();
            error = ex.Number switch
            {
                20303 => Error.NotFound("session.not_found", "Kasa oturumu bulunamadı."),
                20301 => Error.Conflict("session.already_open", "Kasada zaten açık oturum var."),
                20302 => Error.Conflict("session.closed", "Kasa oturumu açık değil."),
                _ => Error.Validation("session.rule", string.IsNullOrWhiteSpace(message) ? "Kasa kuralı ihlali." : message)
            };
            return true;
        }
        error = Error.Failure("session.db", "Veritabanı hatası.");
        return false;
    }
}
