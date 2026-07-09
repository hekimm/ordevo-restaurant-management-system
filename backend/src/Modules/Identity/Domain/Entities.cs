namespace Ordevo.Modules.Identity.Domain;

public sealed class Tenant
{
    public string Id { get; set; } = default!;
    public string Name { get; set; } = default!;
    public string Slug { get; set; } = default!;
    public bool IsActive { get; set; }
}

public sealed class Branch
{
    public string Id { get; set; } = default!;
    public string TenantId { get; set; } = default!;
    public string Name { get; set; } = default!;
    public string Code { get; set; } = default!;
    public string TimeZone { get; set; } = "Europe/Istanbul";
    public string Currency { get; set; } = "TRY";
    public bool IsActive { get; set; }
}

public sealed class Role
{
    public string Id { get; set; } = default!;
    public string TenantId { get; set; } = default!;
    public string Name { get; set; } = default!;
    public string? Description { get; set; }
    public bool IsSystem { get; set; }
}

public sealed class Permission
{
    public string Id { get; set; } = default!;
    public string Code { get; set; } = default!;
    public string? Description { get; set; }
}

public sealed class User
{
    public string Id { get; set; } = default!;
    public string TenantId { get; set; } = default!;
    public string Email { get; set; } = default!;
    public string FullName { get; set; } = default!;
    public string PasswordHash { get; set; } = default!;
    public string? PinHash { get; set; }
    public bool IsActive { get; set; }
    public bool MustChangePassword { get; set; }
    public int FailedAttempts { get; set; }
    public DateTimeOffset? LockedUntil { get; set; }
    public DateTimeOffset? LastLoginAt { get; set; }
}

public sealed class RefreshToken
{
    public string Id { get; set; } = default!;
    public string UserId { get; set; } = default!;
    public string TenantId { get; set; } = default!;
    public string? DeviceId { get; set; }
    public string TokenHash { get; set; } = default!;
    public DateTimeOffset ExpiresAt { get; set; }
    public DateTimeOffset? RevokedAt { get; set; }
    public string? ReplacedByTokenId { get; set; }
    public string? CreatedIp { get; set; }

    public bool IsActive => RevokedAt is null && ExpiresAt > DateTimeOffset.UtcNow;
}
