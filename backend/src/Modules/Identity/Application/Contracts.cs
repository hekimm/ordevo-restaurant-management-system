namespace Ordevo.Modules.Identity.Application;

public sealed record LoginRequest(string TenantSlug, string Email, string Password, string? DeviceFingerprint);
public sealed record RefreshRequest(string RefreshToken);
public sealed record LogoutRequest(string RefreshToken);
public sealed record CreateUserRequest(string Email, string FullName, string Password, string[] Roles, string[]? BranchIds);
public sealed record CreateWaiterRequest(string FullName, string Pin, bool IsActive = true);
public sealed record UpdateUserRequest(string FullName, bool IsActive, string[] Roles, string[]? BranchIds);
public sealed record ResetUserPinRequest(string Pin);
public sealed record ChangePasswordRequest(string CurrentPassword, string NewPassword);

public sealed record TokenPair(
    string AccessToken,
    string RefreshToken,
    DateTimeOffset AccessTokenExpiresAt,
    DateTimeOffset RefreshTokenExpiresAt);

public sealed record AuthResult(TokenPair Tokens, UserProfile User);

public sealed record UserProfile(
    string Id,
    string TenantId,
    string TenantSlug,
    string Email,
    string FullName,
    string[] Roles,
    string[] Permissions,
    string[] BranchIds);

public sealed record UserSummary(
    string Id,
    string Email,
    string FullName,
    bool IsActive,
    string[] Roles);
