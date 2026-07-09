namespace Ordevo.Web.Api;

public sealed record ApiResult<T>(T? Value, int StatusCode, string? Error)
{
    public bool IsSuccess => Error is null;
}

public sealed record LoginRequest(string TenantSlug, string Email, string Password, string? DeviceFingerprint);

public sealed record AuthResult(TokenPair Tokens, UserProfile User);

public sealed record TokenPair(
    string AccessToken,
    string RefreshToken,
    DateTimeOffset AccessTokenExpiresAt,
    DateTimeOffset RefreshTokenExpiresAt);

public sealed record UserProfile(
    string Id,
    string TenantId,
    string TenantSlug,
    string Email,
    string FullName,
    string[] Roles,
    string[] Permissions,
    string[] BranchIds);

public sealed record HealthView(string Name, string Status, bool IsHealthy);
