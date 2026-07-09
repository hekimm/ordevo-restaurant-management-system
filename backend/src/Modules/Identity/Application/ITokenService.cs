using Ordevo.Modules.Identity.Domain;

namespace Ordevo.Modules.Identity.Application;

public sealed record AccessToken(string Value, DateTimeOffset ExpiresAt);
public sealed record RefreshTokenMaterial(string PlainValue, string Hash, DateTimeOffset ExpiresAt);

public interface ITokenService
{
    AccessToken CreateAccessToken(
        User user,
        string tenantSlug,
        IReadOnlyCollection<string> roles,
        IReadOnlyCollection<string> permissions,
        IReadOnlyCollection<string> branchIds,
        string? deviceId);

    RefreshTokenMaterial CreateRefreshToken();

    string HashRefreshToken(string plainValue);
}
