using Dapper;
using Ordevo.BuildingBlocks.Data;
using Ordevo.Modules.Identity.Application;
using Ordevo.Modules.Identity.Domain;

namespace Ordevo.Modules.Identity.Infrastructure;


public sealed class TenantRepository(IDbConnectionFactory factory) : ITenantRepository
{
    public async Task<Tenant?> GetBySlugAsync(string slug, CancellationToken ct = default)
    {
        await using var db = await factory.CreateOpenConnectionAsync(ct);
        return await db.QuerySingleOrDefaultAsync<Tenant>(
            "SELECT ID, NAME, SLUG, IS_ACTIVE FROM TENANTS WHERE SLUG = :slug",
            new OracleParams(new { slug }));
    }

    public async Task<Tenant?> GetByIdAsync(string id, CancellationToken ct = default)
    {
        await using var db = await factory.CreateOpenConnectionAsync(ct);
        return await db.QuerySingleOrDefaultAsync<Tenant>(
            "SELECT ID, NAME, SLUG, IS_ACTIVE FROM TENANTS WHERE ID = :id",
            new OracleParams(new { id }));
    }

    public async Task InsertAsync(Tenant tenant, CancellationToken ct = default)
    {
        await using var db = await factory.CreateOpenConnectionAsync(ct);
        await db.ExecuteAsync(
            "INSERT INTO TENANTS (ID, NAME, SLUG, IS_ACTIVE) VALUES (:Id, :Name, :Slug, :IsActive)",
            new OracleParams(new { tenant.Id, tenant.Name, tenant.Slug, tenant.IsActive }));
    }
}

public sealed class BranchRepository(IDbConnectionFactory factory) : IBranchRepository
{
    public async Task<IReadOnlyList<Branch>> ListByTenantAsync(string tenantId, CancellationToken ct = default)
    {
        await using var db = await factory.CreateOpenConnectionAsync(ct);
        var rows = await db.QueryAsync<Branch>(
            "SELECT ID, TENANT_ID, NAME, CODE, TIME_ZONE, CURRENCY, IS_ACTIVE FROM BRANCHES WHERE TENANT_ID = :tenantId ORDER BY NAME",
            new OracleParams(new { tenantId }));
        return rows.AsList();
    }

    public async Task InsertAsync(Branch branch, CancellationToken ct = default)
    {
        await using var db = await factory.CreateOpenConnectionAsync(ct);
        await db.ExecuteAsync(
            """
            INSERT INTO BRANCHES (ID, TENANT_ID, NAME, CODE, TIME_ZONE, CURRENCY, IS_ACTIVE)
            VALUES (:Id, :TenantId, :Name, :Code, :TimeZone, :Currency, :IsActive)
            """,
            new OracleParams(new { branch.Id, branch.TenantId, branch.Name, branch.Code, branch.TimeZone, branch.Currency, branch.IsActive }));
    }
}

public sealed class RoleRepository(IDbConnectionFactory factory) : IRoleRepository
{
    public async Task<IReadOnlyList<Role>> ListByTenantAsync(string tenantId, CancellationToken ct = default)
    {
        await using var db = await factory.CreateOpenConnectionAsync(ct);
        var rows = await db.QueryAsync<Role>(
            "SELECT ID, TENANT_ID, NAME, DESCRIPTION, IS_SYSTEM FROM ROLES WHERE TENANT_ID = :tenantId ORDER BY NAME",
            new OracleParams(new { tenantId }));
        return rows.AsList();
    }

    public async Task<IReadOnlyList<Role>> GetByNamesAsync(string tenantId, IEnumerable<string> names, CancellationToken ct = default)
    {
        var all = await ListByTenantAsync(tenantId, ct);
        var wanted = names.Select(n => n.Trim().ToLowerInvariant()).ToHashSet();
        return all.Where(r => wanted.Contains(r.Name.ToLowerInvariant())).ToList();
    }

    public async Task InsertAsync(Role role, CancellationToken ct = default)
    {
        await using var db = await factory.CreateOpenConnectionAsync(ct);
        await db.ExecuteAsync(
            """
            INSERT INTO ROLES (ID, TENANT_ID, NAME, DESCRIPTION, IS_SYSTEM)
            VALUES (:Id, :TenantId, :Name, :Description, :IsSystem)
            """,
            new OracleParams(new { role.Id, role.TenantId, role.Name, role.Description, role.IsSystem }));
    }

    public async Task<IReadOnlyList<Permission>> ListAllPermissionsAsync(CancellationToken ct = default)
    {
        await using var db = await factory.CreateOpenConnectionAsync(ct);
        var rows = await db.QueryAsync<Permission>("SELECT ID, CODE, DESCRIPTION FROM PERMISSIONS");
        return rows.AsList();
    }

    public async Task UpsertPermissionAsync(Permission permission, CancellationToken ct = default)
    {
        await using var db = await factory.CreateOpenConnectionAsync(ct);
        await db.ExecuteAsync(
            """
            MERGE INTO PERMISSIONS t
            USING (SELECT :Code AS CODE FROM DUAL) s ON (t.CODE = s.CODE)
            WHEN MATCHED THEN UPDATE SET t.DESCRIPTION = :Description
            WHEN NOT MATCHED THEN INSERT (ID, CODE, DESCRIPTION) VALUES (:Id, :Code, :Description)
            """,
            new OracleParams(new { permission.Id, permission.Code, permission.Description }));
    }

    public async Task SetRolePermissionsAsync(string roleId, IEnumerable<string> permissionIds, CancellationToken ct = default)
    {
        await using var db = await factory.CreateOpenConnectionAsync(ct);
        foreach (var permissionId in permissionIds)
        {
            await db.ExecuteAsync(
                """
                MERGE INTO ROLE_PERMISSIONS t
                USING (SELECT :roleId AS R, :permissionId AS P FROM DUAL) s
                ON (t.ROLE_ID = s.R AND t.PERMISSION_ID = s.P)
                WHEN NOT MATCHED THEN INSERT (ROLE_ID, PERMISSION_ID) VALUES (s.R, s.P)
                """,
                new OracleParams(new { roleId, permissionId }));
        }
    }
}

public sealed class UserRepository(IDbConnectionFactory factory) : IUserRepository
{
    private const string UserColumns =
        "ID, TENANT_ID, EMAIL, FULL_NAME, PASSWORD_HASH, PIN_HASH, IS_ACTIVE, MUST_CHANGE_PASSWORD, FAILED_ATTEMPTS, LOCKED_UNTIL, LAST_LOGIN_AT";

    public async Task<User?> GetByEmailAsync(string tenantId, string email, CancellationToken ct = default)
    {
        await using var db = await factory.CreateOpenConnectionAsync(ct);
        return await db.QuerySingleOrDefaultAsync<User>(
            $"SELECT {UserColumns} FROM USERS WHERE TENANT_ID = :tenantId AND EMAIL = :email",
            new OracleParams(new { tenantId, email }));
    }

    public async Task<User?> GetByIdAsync(string id, CancellationToken ct = default)
    {
        await using var db = await factory.CreateOpenConnectionAsync(ct);
        return await db.QuerySingleOrDefaultAsync<User>(
            $"SELECT {UserColumns} FROM USERS WHERE ID = :id",
            new OracleParams(new { id }));
    }

    public async Task InsertAsync(User user, CancellationToken ct = default)
    {
        await using var db = await factory.CreateOpenConnectionAsync(ct);
        await db.ExecuteAsync(
            """
            INSERT INTO USERS (ID, TENANT_ID, EMAIL, FULL_NAME, PASSWORD_HASH, PIN_HASH, IS_ACTIVE, MUST_CHANGE_PASSWORD, FAILED_ATTEMPTS)
            VALUES (:Id, :TenantId, :Email, :FullName, :PasswordHash, :PinHash, :IsActive, :MustChangePassword, 0)
            """,
            new OracleParams(new
            {
                user.Id, user.TenantId, user.Email, user.FullName,
                user.PasswordHash, user.PinHash, user.IsActive, user.MustChangePassword
            }));
    }

    public async Task UpdateProfileAsync(string userId, string fullName, bool isActive, CancellationToken ct = default)
    {
        await using var db = await factory.CreateOpenConnectionAsync(ct);
        await db.ExecuteAsync(
            """
            UPDATE USERS
               SET FULL_NAME = :fullName,
                   IS_ACTIVE = :isActive,
                   UPDATED_AT = SYSTIMESTAMP
             WHERE ID = :userId
            """,
            new OracleParams(new { userId, fullName, isActive }));
    }

    public async Task UpdatePasswordAsync(string userId, string passwordHash, CancellationToken ct = default)
    {
        await using var db = await factory.CreateOpenConnectionAsync(ct);
        await db.ExecuteAsync(
            "UPDATE USERS SET PASSWORD_HASH = :passwordHash, MUST_CHANGE_PASSWORD = 0, UPDATED_AT = SYSTIMESTAMP WHERE ID = :userId",
            new OracleParams(new { userId, passwordHash }));
    }

    public async Task UpdatePinAsync(string userId, string passwordHash, string pinHash, CancellationToken ct = default)
    {
        await using var db = await factory.CreateOpenConnectionAsync(ct);
        await db.ExecuteAsync(
            """
            UPDATE USERS
               SET PASSWORD_HASH = :passwordHash,
                   PIN_HASH = :pinHash,
                   MUST_CHANGE_PASSWORD = 0,
                   UPDATED_AT = SYSTIMESTAMP
             WHERE ID = :userId
            """,
            new OracleParams(new { userId, passwordHash, pinHash }));
    }

    public async Task SetActiveAsync(string userId, bool isActive, CancellationToken ct = default)
    {
        await using var db = await factory.CreateOpenConnectionAsync(ct);
        await db.ExecuteAsync(
            "UPDATE USERS SET IS_ACTIVE = :isActive, UPDATED_AT = SYSTIMESTAMP WHERE ID = :userId",
            new OracleParams(new { userId, isActive }));
    }

    public async Task RecordLoginSuccessAsync(string userId, CancellationToken ct = default)
    {
        await using var db = await factory.CreateOpenConnectionAsync(ct);
        await db.ExecuteAsync(
            "UPDATE USERS SET FAILED_ATTEMPTS = 0, LOCKED_UNTIL = NULL, LAST_LOGIN_AT = SYSTIMESTAMP, UPDATED_AT = SYSTIMESTAMP WHERE ID = :userId",
            new OracleParams(new { userId }));
    }

    public async Task RecordLoginFailureAsync(string userId, int lockThreshold, int lockMinutes, CancellationToken ct = default)
    {
        await using var db = await factory.CreateOpenConnectionAsync(ct);
        await db.ExecuteAsync(
            """
            UPDATE USERS
               SET FAILED_ATTEMPTS = FAILED_ATTEMPTS + 1,
                   LOCKED_UNTIL = CASE WHEN FAILED_ATTEMPTS + 1 >= :lockThreshold
                                       THEN SYSTIMESTAMP + NUMTODSINTERVAL(:lockMinutes, 'MINUTE')
                                       ELSE LOCKED_UNTIL END,
                   UPDATED_AT = SYSTIMESTAMP
             WHERE ID = :userId
            """,
            new OracleParams(new { userId, lockThreshold, lockMinutes }));
    }

    public async Task<IReadOnlyList<string>> GetRoleNamesAsync(string userId, CancellationToken ct = default)
    {
        await using var db = await factory.CreateOpenConnectionAsync(ct);
        var rows = await db.QueryAsync<string>(
            """
            SELECT r.NAME FROM USER_ROLES ur
            JOIN ROLES r ON r.ID = ur.ROLE_ID
            WHERE ur.USER_ID = :userId ORDER BY r.NAME
            """,
            new OracleParams(new { userId }));
        return rows.AsList();
    }

    public async Task<IReadOnlyList<string>> GetPermissionCodesAsync(string userId, CancellationToken ct = default)
    {
        await using var db = await factory.CreateOpenConnectionAsync(ct);
        var rows = await db.QueryAsync<string>(
            """
            SELECT DISTINCT p.CODE
            FROM USER_ROLES ur
            JOIN ROLE_PERMISSIONS rp ON rp.ROLE_ID = ur.ROLE_ID
            JOIN PERMISSIONS p ON p.ID = rp.PERMISSION_ID
            WHERE ur.USER_ID = :userId
            """,
            new OracleParams(new { userId }));
        return rows.AsList();
    }

    public async Task<IReadOnlyList<string>> GetBranchIdsAsync(string userId, CancellationToken ct = default)
    {
        await using var db = await factory.CreateOpenConnectionAsync(ct);
        var rows = await db.QueryAsync<string>(
            "SELECT BRANCH_ID FROM USER_BRANCHES WHERE USER_ID = :userId",
            new OracleParams(new { userId }));
        return rows.AsList();
    }

    public async Task AssignRolesAsync(string userId, IEnumerable<string> roleIds, CancellationToken ct = default)
    {
        await using var db = await factory.CreateOpenConnectionAsync(ct);
        foreach (var roleId in roleIds)
        {
            await db.ExecuteAsync(
                """
                MERGE INTO USER_ROLES t
                USING (SELECT :userId AS U, :roleId AS R FROM DUAL) s
                ON (t.USER_ID = s.U AND t.ROLE_ID = s.R)
                WHEN NOT MATCHED THEN INSERT (USER_ID, ROLE_ID) VALUES (s.U, s.R)
                """,
                new OracleParams(new { userId, roleId }));
        }
    }

    public async Task ReplaceRolesAsync(string userId, IEnumerable<string> roleIds, CancellationToken ct = default)
    {
        await using var db = await factory.CreateOpenConnectionAsync(ct);
        await db.ExecuteAsync("DELETE FROM USER_ROLES WHERE USER_ID = :userId", new OracleParams(new { userId }));
        foreach (var roleId in roleIds)
        {
            await db.ExecuteAsync(
                """
                MERGE INTO USER_ROLES t
                USING (SELECT :userId AS U, :roleId AS R FROM DUAL) s
                ON (t.USER_ID = s.U AND t.ROLE_ID = s.R)
                WHEN NOT MATCHED THEN INSERT (USER_ID, ROLE_ID) VALUES (s.U, s.R)
                """,
                new OracleParams(new { userId, roleId }));
        }
    }

    public async Task AssignBranchesAsync(string userId, IEnumerable<string> branchIds, CancellationToken ct = default)
    {
        await using var db = await factory.CreateOpenConnectionAsync(ct);
        foreach (var branchId in branchIds)
        {
            await db.ExecuteAsync(
                """
                MERGE INTO USER_BRANCHES t
                USING (SELECT :userId AS U, :branchId AS B FROM DUAL) s
                ON (t.USER_ID = s.U AND t.BRANCH_ID = s.B)
                WHEN NOT MATCHED THEN INSERT (USER_ID, BRANCH_ID) VALUES (s.U, s.B)
                """,
                new OracleParams(new { userId, branchId }));
        }
    }

    public async Task ReplaceBranchesAsync(string userId, IEnumerable<string> branchIds, CancellationToken ct = default)
    {
        await using var db = await factory.CreateOpenConnectionAsync(ct);
        await db.ExecuteAsync("DELETE FROM USER_BRANCHES WHERE USER_ID = :userId", new OracleParams(new { userId }));
        foreach (var branchId in branchIds)
        {
            await db.ExecuteAsync(
                """
                MERGE INTO USER_BRANCHES t
                USING (SELECT :userId AS U, :branchId AS B FROM DUAL) s
                ON (t.USER_ID = s.U AND t.BRANCH_ID = s.B)
                WHEN NOT MATCHED THEN INSERT (USER_ID, BRANCH_ID) VALUES (s.U, s.B)
                """,
                new OracleParams(new { userId, branchId }));
        }
    }

    public async Task<IReadOnlyList<UserSummary>> ListAsync(string tenantId, CancellationToken ct = default)
    {
        await using var db = await factory.CreateOpenConnectionAsync(ct);
        var users = (await db.QueryAsync<(string Id, string Email, string FullName, bool IsActive)>(
            "SELECT ID, EMAIL, FULL_NAME, IS_ACTIVE FROM USERS WHERE TENANT_ID = :tenantId ORDER BY FULL_NAME",
            new OracleParams(new { tenantId }))).AsList();

        var result = new List<UserSummary>(users.Count);
        foreach (var u in users)
        {
            var roles = await GetRoleNamesAsync(u.Id, ct);
            result.Add(new UserSummary(u.Id, u.Email, u.FullName, u.IsActive, [.. roles]));
        }
        return result;
    }
}

public sealed class SettingsRepository(IDbConnectionFactory factory) : ISettingsRepository
{
    public async Task<string?> GetValueAsync(string tenantId, string? branchId, string key, CancellationToken ct = default)
    {
        await using var db = await factory.CreateOpenConnectionAsync(ct);
        return await db.QuerySingleOrDefaultAsync<string>(
            """
            SELECT SETTING_VALUE
            FROM (
                SELECT SETTING_VALUE,
                       CASE
                         WHEN BRANCH_ID = :branchId THEN 0
                         WHEN BRANCH_ID IS NULL THEN 1
                         ELSE 2
                       END AS PRIORITY
                FROM PLATFORM_SETTINGS
                WHERE TENANT_ID = :tenantId
                  AND SETTING_KEY = :key
                  AND IS_ACTIVE = 1
                  AND (BRANCH_ID = :branchId OR BRANCH_ID IS NULL)
                ORDER BY PRIORITY
            )
            WHERE ROWNUM = 1
            """,
            new OracleParams(new { tenantId, branchId, key }));
    }

    public async Task UpsertValueAsync(
        string tenantId,
        string? branchId,
        string key,
        string? value,
        string userId,
        CancellationToken ct = default)
    {
        await using var db = await factory.CreateOpenConnectionAsync(ct);
        var id = Guid.NewGuid().ToString();

        await db.ExecuteAsync(
            """
            MERGE INTO PLATFORM_SETTINGS t
            USING (
                SELECT :tenantId AS TENANT_ID, :branchId AS BRANCH_ID, :key AS SETTING_KEY FROM DUAL
            ) s
            ON (
                t.TENANT_ID = s.TENANT_ID
                AND t.SETTING_KEY = s.SETTING_KEY
                AND (
                    (t.BRANCH_ID = s.BRANCH_ID)
                    OR (t.BRANCH_ID IS NULL AND s.BRANCH_ID IS NULL)
                )
            )
            WHEN MATCHED THEN UPDATE SET
                t.SETTING_VALUE = :value,
                t.IS_ACTIVE = 1,
                t.UPDATED_AT = SYSTIMESTAMP,
                t.UPDATED_BY = :userId,
                t.ROW_VERSION = t.ROW_VERSION + 1
            WHEN NOT MATCHED THEN INSERT (
                ID, TENANT_ID, BRANCH_ID, SETTING_KEY, SETTING_VALUE,
                IS_ACTIVE, CREATED_BY, UPDATED_BY
            ) VALUES (
                :id, :tenantId, :branchId, :key, :value,
                1, :userId, :userId
            )
            """,
            new OracleParams(new { id, tenantId, branchId, key, value, userId }));
    }
}

public sealed class RefreshTokenRepository(IDbConnectionFactory factory) : IRefreshTokenRepository
{
    public async Task InsertAsync(RefreshToken token, CancellationToken ct = default)
    {
        await using var db = await factory.CreateOpenConnectionAsync(ct);
        await db.ExecuteAsync(
            """
            INSERT INTO REFRESH_TOKENS (ID, USER_ID, TENANT_ID, DEVICE_ID, TOKEN_HASH, EXPIRES_AT, CREATED_IP)
            VALUES (:Id, :UserId, :TenantId, :DeviceId, :TokenHash, :ExpiresAt, :CreatedIp)
            """,
            new OracleParams(new
            {
                token.Id, token.UserId, token.TenantId, token.DeviceId,
                token.TokenHash, token.ExpiresAt, token.CreatedIp
            }));
    }

    public async Task<RefreshToken?> GetByHashAsync(string tokenHash, CancellationToken ct = default)
    {
        await using var db = await factory.CreateOpenConnectionAsync(ct);
        return await db.QuerySingleOrDefaultAsync<RefreshToken>(
            """
            SELECT ID, USER_ID, TENANT_ID, DEVICE_ID, TOKEN_HASH, EXPIRES_AT, REVOKED_AT, REPLACED_BY_TOKEN_ID, CREATED_IP
            FROM REFRESH_TOKENS WHERE TOKEN_HASH = :tokenHash
            """,
            new OracleParams(new { tokenHash }));
    }

    public async Task RevokeAsync(string tokenId, string? replacedByTokenId, CancellationToken ct = default)
    {
        await using var db = await factory.CreateOpenConnectionAsync(ct);
        await db.ExecuteAsync(
            """
            UPDATE REFRESH_TOKENS
               SET REVOKED_AT = SYSTIMESTAMP, REPLACED_BY_TOKEN_ID = :replacedByTokenId
             WHERE ID = :tokenId AND REVOKED_AT IS NULL
            """,
            new OracleParams(new { tokenId, replacedByTokenId }));
    }

    public async Task RevokeAllForUserAsync(string userId, CancellationToken ct = default)
    {
        await using var db = await factory.CreateOpenConnectionAsync(ct);
        await db.ExecuteAsync(
            "UPDATE REFRESH_TOKENS SET REVOKED_AT = SYSTIMESTAMP WHERE USER_ID = :userId AND REVOKED_AT IS NULL",
            new OracleParams(new { userId }));
    }
}

public sealed class AuditWriter(IDbConnectionFactory factory) : IAuditWriter
{
    public async Task WriteAsync(
        string? tenantId, string? userId, string action,
        string? entityType = null, string? entityId = null,
        string? detailJson = null, string? ip = null,
        CancellationToken ct = default)
    {
        await using var db = await factory.CreateOpenConnectionAsync(ct);
        await db.ExecuteAsync(
            """
            INSERT INTO AUDIT_LOG (ID, TENANT_ID, USER_ID, ACTION, ENTITY_TYPE, ENTITY_ID, DETAIL, IP_ADDRESS)
            VALUES (:Id, :tenantId, :userId, :action, :entityType, :entityId, :detailJson, :ip)
            """,
            new OracleParams(new
            {
                Id = Guid.NewGuid().ToString(),
                tenantId, userId, action, entityType, entityId, detailJson, ip
            }));
    }
}
