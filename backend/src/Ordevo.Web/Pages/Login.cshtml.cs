using System.ComponentModel.DataAnnotations;
using System.Security.Claims;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Ordevo.Web.Api;
using Ordevo.Web.Models;

namespace Ordevo.Web.Pages;

public sealed class LoginModel(OrdevoApiClient api) : PageModel
{
    [BindProperty]
    public LoginInput Input { get; set; } = new();

    public string? Error { get; private set; }

    public void OnGet()
    {
        if (string.IsNullOrWhiteSpace(Input.TenantSlug))
            Input.TenantSlug = "demo";
    }

    public async Task<IActionResult> OnPostAsync(CancellationToken ct)
    {
        if (!ModelState.IsValid)
            return Page();

        var login = new LoginRequest(
            Input.TenantSlug.Trim(),
            Input.Email.Trim(),
            Input.Password,
            DeviceFingerprint: $"web:{Environment.MachineName}");

        var result = await api.LoginAsync(login, ct);
        if (!result.IsSuccess || result.Value is null)
        {
            Error = UiFormat.Error(result);
            return Page();
        }

        var auth = result.Value;
        var claims = new List<Claim>
        {
            new(ClaimTypes.NameIdentifier, auth.User.Id),
            new(ClaimTypes.Email, auth.User.Email),
            new(ClaimTypes.Name, auth.User.FullName),
            new("name", auth.User.FullName),
            new("tenant_id", auth.User.TenantId),
            new("tenant_slug", auth.User.TenantSlug)
        };
        claims.AddRange(auth.User.Roles.Select(role => new Claim(ClaimTypes.Role, role)));
        claims.AddRange(auth.User.Permissions.Select(permission => new Claim("perm", permission)));
        claims.AddRange(auth.User.BranchIds.Select(branchId => new Claim("branch", branchId)));

        var identity = new ClaimsIdentity(claims, CookieAuthenticationDefaults.AuthenticationScheme);
        var principal = new ClaimsPrincipal(identity);
        var properties = new AuthenticationProperties
        {
            IsPersistent = Input.RememberMe,
            ExpiresUtc = auth.Tokens.RefreshTokenExpiresAt
        };
        properties.StoreTokens(
        [
            new AuthenticationToken { Name = "access_token", Value = auth.Tokens.AccessToken },
            new AuthenticationToken { Name = "refresh_token", Value = auth.Tokens.RefreshToken },
            new AuthenticationToken { Name = "expires_at", Value = auth.Tokens.AccessTokenExpiresAt.ToString("O") }
        ]);

        await HttpContext.SignInAsync(CookieAuthenticationDefaults.AuthenticationScheme, principal, properties);
        return LocalRedirect(Url.Content("~/dashboard"));
    }

    public sealed class LoginInput
    {
        [Required]
        public string TenantSlug { get; set; } = "demo";

        [Required, EmailAddress]
        public string Email { get; set; } = "owner@ordevo.local";

        [Required, DataType(DataType.Password)]
        public string Password { get; set; } = "";

        public bool RememberMe { get; set; } = true;
    }
}
