using Microsoft.Extensions.Options;
using Ordevo.BuildingBlocks.Results;
using Ordevo.Modules.Identity.Domain;

namespace Ordevo.Modules.Identity.Application;

public sealed class AuthService(
    ITenantRepository tenants,
    IUserRepository users,
    IRefreshTokenRepository refreshTokens,
    ITokenService tokenService,
    IPasswordHasher passwordHasher,
    IAuditWriter audit,
    IOptions<IdentityOptions> options)
{
    private readonly IdentityOptions _options = options.Value;
    private static readonly Error InvalidCredentials =
        Error.Unauthorized("auth.invalid_credentials", "E-posta veya şifre hatalı.");

    public async Task<Result<AuthResult>> LoginAsync(LoginRequest request, string? ip, CancellationToken ct = default)
    {
        var tenant = await tenants.GetBySlugAsync(request.TenantSlug, ct);
        if (tenant is null || !tenant.IsActive)
            return InvalidCredentials;

        var email = request.Email.Trim().ToLowerInvariant();
        var user = await users.GetByEmailAsync(tenant.Id, email, ct);
        if (user is null)
            return InvalidCredentials;

        if (user.LockedUntil is { } locked && locked > DateTimeOffset.UtcNow)
            return Error.Forbidden("auth.locked", "Hesap geçici olarak kilitli. Lütfen sonra tekrar deneyin.");

        if (!user.IsActive)
            return Error.Forbidden("auth.inactive", "Hesap pasif.");

        if (!passwordHasher.Verify(request.Password, user.PasswordHash))
        {
            await users.RecordLoginFailureAsync(user.Id, _options.LockThreshold, _options.LockMinutes, ct);
            await audit.WriteAsync(tenant.Id, user.Id, "auth.login_failed", "user", user.Id, ip: ip, ct: ct);
            return InvalidCredentials;
        }

        var result = await IssueAsync(tenant, user, deviceId: null, ip, ct);
        await users.RecordLoginSuccessAsync(user.Id, ct);
        await audit.WriteAsync(tenant.Id, user.Id, "auth.login", "user", user.Id, ip: ip, ct: ct);
        return result;
    }

    public async Task<Result<AuthResult>> RefreshAsync(string presentedToken, string? ip, CancellationToken ct = default)
    {
        var hash = tokenService.HashRefreshToken(presentedToken);
        var stored = await refreshTokens.GetByHashAsync(hash, ct);
        if (stored is null)
            return Error.Unauthorized("auth.invalid_token", "Oturum geçersiz.");

        if (stored.RevokedAt is not null)
        {
            await refreshTokens.RevokeAllForUserAsync(stored.UserId, ct);
            await audit.WriteAsync(stored.TenantId, stored.UserId, "auth.refresh_reuse_detected", "user", stored.UserId, ip: ip, ct: ct);
            return Error.Unauthorized("auth.invalid_token", "Oturum geçersiz.");
        }

        if (stored.ExpiresAt <= DateTimeOffset.UtcNow)
            return Error.Unauthorized("auth.expired_token", "Oturum süresi doldu.");

        var user = await users.GetByIdAsync(stored.UserId, ct);
        var tenant = user is null ? null : await tenants.GetByIdAsync(user.TenantId, ct);
        if (user is null || tenant is null || !user.IsActive || !tenant.IsActive)
            return Error.Unauthorized("auth.invalid_token", "Oturum geçersiz.");

        var result = await IssueAsync(tenant, user, stored.DeviceId, ip, ct, replacesTokenId: stored.Id);
        await audit.WriteAsync(tenant.Id, user.Id, "auth.refresh", "user", user.Id, ip: ip, ct: ct);
        return result;
    }

    public async Task<Result> LogoutAsync(string presentedToken, CancellationToken ct = default)
    {
        var hash = tokenService.HashRefreshToken(presentedToken);
        var stored = await refreshTokens.GetByHashAsync(hash, ct);
        if (stored is not null && stored.RevokedAt is null)
        {
            await refreshTokens.RevokeAsync(stored.Id, null, ct);
            await audit.WriteAsync(stored.TenantId, stored.UserId, "auth.logout", "user", stored.UserId, ct: ct);
        }
        return Result.Success();
    }

    private async Task<AuthResult> IssueAsync(
        Tenant tenant, User user, string? deviceId, string? ip,
        CancellationToken ct, string? replacesTokenId = null)
    {
        var roles = await users.GetRoleNamesAsync(user.Id, ct);
        var permissions = await users.GetPermissionCodesAsync(user.Id, ct);
        var branchIds = await users.GetBranchIdsAsync(user.Id, ct);

        var access = tokenService.CreateAccessToken(user, tenant.Slug, roles, permissions, branchIds, deviceId);
        var refresh = tokenService.CreateRefreshToken();

        var refreshEntity = new RefreshToken
        {
            Id = Guid.NewGuid().ToString(),
            UserId = user.Id,
            TenantId = tenant.Id,
            DeviceId = deviceId,
            TokenHash = refresh.Hash,
            ExpiresAt = refresh.ExpiresAt,
            CreatedIp = ip
        };
        await refreshTokens.InsertAsync(refreshEntity, ct);

        if (replacesTokenId is not null)
            await refreshTokens.RevokeAsync(replacesTokenId, refreshEntity.Id, ct);

        var profile = new UserProfile(
            user.Id, tenant.Id, tenant.Slug, user.Email, user.FullName,
            [.. roles], [.. permissions], [.. branchIds]);

        var pair = new TokenPair(access.Value, refresh.PlainValue, access.ExpiresAt, refresh.ExpiresAt);
        return new AuthResult(pair, profile);
    }
}
