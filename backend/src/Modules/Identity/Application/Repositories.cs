using Ordevo.Modules.Identity.Domain;

namespace Ordevo.Modules.Identity.Application;

public interface ITenantRepository
{
    Task<Tenant?> GetBySlugAsync(string slug, CancellationToken ct = default);
    Task<Tenant?> GetByIdAsync(string id, CancellationToken ct = default);
    Task InsertAsync(Tenant tenant, CancellationToken ct = default);
}

public interface IBranchRepository
{
    Task<IReadOnlyList<Branch>> ListByTenantAsync(string tenantId, CancellationToken ct = default);
    Task InsertAsync(Branch branch, CancellationToken ct = default);
}

public interface IRoleRepository
{
    Task<IReadOnlyList<Role>> ListByTenantAsync(string tenantId, CancellationToken ct = default);
    Task<IReadOnlyList<Role>> GetByNamesAsync(string tenantId, IEnumerable<string> names, CancellationToken ct = default);
    Task InsertAsync(Role role, CancellationToken ct = default);
    Task<IReadOnlyList<Permission>> ListAllPermissionsAsync(CancellationToken ct = default);
    Task UpsertPermissionAsync(Permission permission, CancellationToken ct = default);
    Task SetRolePermissionsAsync(string roleId, IEnumerable<string> permissionIds, CancellationToken ct = default);
}

public interface IUserRepository
{
    Task<User?> GetByEmailAsync(string tenantId, string email, CancellationToken ct = default);
    Task<User?> GetByIdAsync(string id, CancellationToken ct = default);
    Task InsertAsync(User user, CancellationToken ct = default);
    Task UpdateProfileAsync(string userId, string fullName, bool isActive, CancellationToken ct = default);
    Task UpdatePasswordAsync(string userId, string passwordHash, CancellationToken ct = default);
    Task UpdatePinAsync(string userId, string passwordHash, string pinHash, CancellationToken ct = default);
    Task SetActiveAsync(string userId, bool isActive, CancellationToken ct = default);
    Task RecordLoginSuccessAsync(string userId, CancellationToken ct = default);
    Task RecordLoginFailureAsync(string userId, int lockThreshold, int lockMinutes, CancellationToken ct = default);

    Task<IReadOnlyList<string>> GetRoleNamesAsync(string userId, CancellationToken ct = default);
    Task<IReadOnlyList<string>> GetPermissionCodesAsync(string userId, CancellationToken ct = default);
    Task<IReadOnlyList<string>> GetBranchIdsAsync(string userId, CancellationToken ct = default);

    Task AssignRolesAsync(string userId, IEnumerable<string> roleIds, CancellationToken ct = default);
    Task ReplaceRolesAsync(string userId, IEnumerable<string> roleIds, CancellationToken ct = default);
    Task AssignBranchesAsync(string userId, IEnumerable<string> branchIds, CancellationToken ct = default);
    Task ReplaceBranchesAsync(string userId, IEnumerable<string> branchIds, CancellationToken ct = default);

    Task<IReadOnlyList<UserSummary>> ListAsync(string tenantId, CancellationToken ct = default);
}

public interface ISettingsRepository
{
    Task<string?> GetValueAsync(string tenantId, string? branchId, string key, CancellationToken ct = default);

    Task UpsertValueAsync(
        string tenantId,
        string? branchId,
        string key,
        string? value,
        string userId,
        CancellationToken ct = default);
}

public interface IRefreshTokenRepository
{
    Task InsertAsync(RefreshToken token, CancellationToken ct = default);
    Task<RefreshToken?> GetByHashAsync(string tokenHash, CancellationToken ct = default);
    Task RevokeAsync(string tokenId, string? replacedByTokenId, CancellationToken ct = default);
    Task RevokeAllForUserAsync(string userId, CancellationToken ct = default);
}

public interface IAuditWriter
{
    Task WriteAsync(
        string? tenantId, string? userId, string action,
        string? entityType = null, string? entityId = null,
        string? detailJson = null, string? ip = null,
        CancellationToken ct = default);
}
