using Ordevo.Desktop.Wpf.Models;

namespace Ordevo.Desktop.Wpf.Services;

public sealed class DesktopSession
{
    public string? AccessToken { get; private set; }
    public string? RefreshToken { get; private set; }
    public DateTimeOffset AccessTokenExpiresAt { get; private set; }
    public UserProfile? User { get; private set; }

    public bool IsAuthenticated => !string.IsNullOrWhiteSpace(AccessToken) && User is not null;

    public void SignIn(AuthResult auth)
    {
        AccessToken = auth.Tokens.AccessToken;
        RefreshToken = auth.Tokens.RefreshToken;
        AccessTokenExpiresAt = auth.Tokens.AccessTokenExpiresAt;
        User = auth.User;
    }

    public void SignOut()
    {
        AccessToken = null;
        RefreshToken = null;
        AccessTokenExpiresAt = default;
        User = null;
    }
}
