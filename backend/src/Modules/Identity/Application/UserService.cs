using Ordevo.BuildingBlocks.Results;
using Ordevo.Modules.Identity.Domain;

namespace Ordevo.Modules.Identity.Application;

public sealed class UserService(
    IUserRepository users,
    IRoleRepository roles,
    IBranchRepository branches,
    IPasswordHasher passwordHasher,
    IAuditWriter audit)
{
    public async Task<Result<UserSummary>> CreateAsync(
        string tenantId, string actingUserId, CreateUserRequest request, CancellationToken ct = default)
    {
        var email = request.Email.Trim().ToLowerInvariant();

        if (await users.GetByEmailAsync(tenantId, email, ct) is not null)
            return Error.Conflict("user.email_exists", "Bu e-posta ile bir kullanıcı zaten var.");

        var matchedRoles = await roles.GetByNamesAsync(tenantId, request.Roles, ct);
        if (matchedRoles.Count == 0)
            return Error.Validation("user.no_roles", "En az bir geçerli rol belirtilmeli.");

        var user = new User
        {
            Id = Guid.NewGuid().ToString(),
            TenantId = tenantId,
            Email = email,
            FullName = request.FullName.Trim(),
            PasswordHash = passwordHasher.Hash(request.Password),
            IsActive = true,
            MustChangePassword = true
        };
        await users.InsertAsync(user, ct);
        await users.AssignRolesAsync(user.Id, matchedRoles.Select(r => r.Id), ct);

        var branchIds = request.BranchIds is { Length: > 0 }
            ? request.BranchIds
            : (await branches.ListByTenantAsync(tenantId, ct)).Select(b => b.Id).ToArray();
        await users.AssignBranchesAsync(user.Id, branchIds, ct);

        await audit.WriteAsync(tenantId, actingUserId, "user.create", "user", user.Id, ct: ct);

        return new UserSummary(user.Id, user.Email, user.FullName, user.IsActive, [.. matchedRoles.Select(r => r.Name)]);
    }

    public async Task<Result<UserSummary>> CreateWaiterAsync(
        string tenantId, string actingUserId, CreateWaiterRequest request, CancellationToken ct = default)
    {
        var fullName = NormalizeFullName(request.FullName);
        if (fullName is null)
            return Error.Validation("user.name_required", "Garson adı zorunlu.");

        if (!IsSixDigitPin(request.Pin))
            return Error.Validation("user.pin_invalid", "PIN 6 haneli rakamlardan oluşmalı.");

        var matchedRoles = await roles.GetByNamesAsync(tenantId, [SystemRoles.Waiter], ct);
        if (matchedRoles.Count == 0)
            return Error.Validation("user.no_roles", "Garson rolü bulunamadı.");

        var pinHash = passwordHasher.Hash(request.Pin);
        var user = new User
        {
            Id = Guid.NewGuid().ToString(),
            TenantId = tenantId,
            Email = $"waiter-{Guid.NewGuid():N}@ordevo.local",
            FullName = fullName,
            PasswordHash = pinHash,
            PinHash = pinHash,
            IsActive = request.IsActive,
            MustChangePassword = false
        };

        await users.InsertAsync(user, ct);
        await users.AssignRolesAsync(user.Id, matchedRoles.Select(r => r.Id), ct);
        await users.AssignBranchesAsync(user.Id, await BranchesOrDefaultAsync(tenantId, null, ct), ct);
        await audit.WriteAsync(tenantId, actingUserId, "waiter.create", "user", user.Id, ct: ct);

        return new UserSummary(user.Id, user.Email, user.FullName, user.IsActive, [SystemRoles.Waiter]);
    }

    public Task<IReadOnlyList<UserSummary>> ListAsync(string tenantId, CancellationToken ct = default)
        => users.ListAsync(tenantId, ct);

    public async Task<Result<UserSummary>> UpdateAsync(
        string tenantId, string actingUserId, string userId, UpdateUserRequest request, CancellationToken ct = default)
    {
        var user = await users.GetByIdAsync(userId, ct);
        if (user is null || user.TenantId != tenantId)
            return Error.NotFound("user.not_found", "Kullanıcı bulunamadı.");

        var fullName = NormalizeFullName(request.FullName);
        if (fullName is null)
            return Error.Validation("user.name_required", "Personel adı zorunlu.");

        var requestedRoleNames = (request.Roles ?? [])
            .Where(x => !string.IsNullOrWhiteSpace(x))
            .Select(x => x.Trim())
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToArray();

        if (requestedRoleNames.Length == 0)
            return Error.Validation("user.no_roles", "En az bir rol seçilmeli.");

        var matchedRoles = await roles.GetByNamesAsync(tenantId, requestedRoleNames, ct);
        if (matchedRoles.Count != requestedRoleNames.Length)
            return Error.Validation("user.invalid_roles", "Seçilen rollerden biri kullanılamıyor.");

        await users.UpdateProfileAsync(userId, fullName, request.IsActive, ct);
        await users.ReplaceRolesAsync(userId, matchedRoles.Select(r => r.Id), ct);
        await users.ReplaceBranchesAsync(userId, await BranchesOrDefaultAsync(tenantId, request.BranchIds, ct), ct);
        await audit.WriteAsync(tenantId, actingUserId, "user.update", "user", userId, ct: ct);

        return new UserSummary(userId, user.Email, fullName, request.IsActive, [.. matchedRoles.Select(r => r.Name)]);
    }

    public async Task<Result> ResetPinAsync(
        string tenantId, string actingUserId, string userId, ResetUserPinRequest request, CancellationToken ct = default)
    {
        var user = await users.GetByIdAsync(userId, ct);
        if (user is null || user.TenantId != tenantId)
            return Error.NotFound("user.not_found", "Kullanıcı bulunamadı.");

        if (!IsSixDigitPin(request.Pin))
            return Error.Validation("user.pin_invalid", "PIN 6 haneli rakamlardan oluşmalı.");

        var hash = passwordHasher.Hash(request.Pin);
        await users.UpdatePinAsync(userId, hash, hash, ct);
        await audit.WriteAsync(tenantId, actingUserId, "user.pin_reset", "user", userId, ct: ct);
        return Result.Success();
    }

    public async Task<Result> DeactivateAsync(
        string tenantId, string actingUserId, string userId, CancellationToken ct = default)
    {
        var user = await users.GetByIdAsync(userId, ct);
        if (user is null || user.TenantId != tenantId)
            return Error.NotFound("user.not_found", "Kullanıcı bulunamadı.");

        await users.SetActiveAsync(userId, false, ct);
        await audit.WriteAsync(tenantId, actingUserId, "user.deactivate", "user", userId, ct: ct);
        return Result.Success();
    }

    public async Task<Result> ChangePasswordAsync(
        string userId, ChangePasswordRequest request, CancellationToken ct = default)
    {
        var user = await users.GetByIdAsync(userId, ct);
        if (user is null)
            return Error.NotFound("user.not_found", "Kullanıcı bulunamadı.");

        if (!passwordHasher.Verify(request.CurrentPassword, user.PasswordHash))
            return Error.Validation("user.bad_current_password", "Mevcut şifre hatalı.");

        await users.UpdatePasswordAsync(userId, passwordHasher.Hash(request.NewPassword), ct);
        await audit.WriteAsync(user.TenantId, userId, "user.change_password", "user", userId, ct: ct);
        return Result.Success();
    }

    private async Task<string[]> BranchesOrDefaultAsync(string tenantId, string[]? submittedBranchIds, CancellationToken ct)
    {
        if (submittedBranchIds is { Length: > 0 })
            return [.. submittedBranchIds.Where(x => !string.IsNullOrWhiteSpace(x)).Distinct(StringComparer.OrdinalIgnoreCase)];

        return [.. (await branches.ListByTenantAsync(tenantId, ct)).Select(b => b.Id)];
    }

    private static string? NormalizeFullName(string? value)
    {
        var normalized = value?.Trim();
        return string.IsNullOrWhiteSpace(normalized) ? null : normalized;
    }

    private static bool IsSixDigitPin(string? value)
        => value is { Length: 6 } && value.All(char.IsDigit);
}
