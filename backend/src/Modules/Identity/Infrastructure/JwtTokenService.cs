using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;
using Ordevo.BuildingBlocks.Auth;
using Ordevo.BuildingBlocks.Multitenancy;
using Ordevo.Modules.Identity.Application;
using Ordevo.Modules.Identity.Domain;

namespace Ordevo.Modules.Identity.Infrastructure;

public sealed class JwtTokenService : ITokenService
{
    private readonly JwtOptions _options;
    private readonly SigningCredentials _credentials;

    public JwtTokenService(IOptions<JwtOptions> options)
    {
        _options = options.Value;
        if (string.IsNullOrWhiteSpace(_options.SigningKey) || _options.SigningKey.Length < 32)
            throw new InvalidOperationException("Jwt:SigningKey must be configured with at least 32 characters.");
        var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_options.SigningKey));
        _credentials = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);
    }

    public AccessToken CreateAccessToken(
        User user,
        string tenantSlug,
        IReadOnlyCollection<string> roles,
        IReadOnlyCollection<string> permissions,
        IReadOnlyCollection<string> branchIds,
        string? deviceId)
    {
        var now = DateTimeOffset.UtcNow;
        var expires = now.AddMinutes(_options.AccessTokenMinutes);

        var claims = new List<Claim>
        {
            new(JwtRegisteredClaimNames.Sub, user.Id),
            new(ClaimTypes.NameIdentifier, user.Id),
            new(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString("N")),
            new(JwtRegisteredClaimNames.Email, user.Email),
            new("name", user.FullName),
            new(TenantContext.TenantClaim, user.TenantId),
            new("tenant_slug", tenantSlug)
        };

        var primaryBranch = branchIds.FirstOrDefault();
        if (primaryBranch is not null)
            claims.Add(new Claim(TenantContext.BranchClaim, primaryBranch));
        foreach (var branchId in branchIds)
            claims.Add(new Claim("branch", branchId));
        if (deviceId is not null)
            claims.Add(new Claim(TenantContext.DeviceClaim, deviceId));

        foreach (var role in roles)
            claims.Add(new Claim(ClaimTypes.Role, role));
        foreach (var permission in permissions)
            claims.Add(new Claim("perm", permission));

        var token = new JwtSecurityToken(
            issuer: _options.Issuer,
            audience: _options.Audience,
            claims: claims,
            notBefore: now.UtcDateTime,
            expires: expires.UtcDateTime,
            signingCredentials: _credentials);

        var value = new JwtSecurityTokenHandler().WriteToken(token);
        return new AccessToken(value, expires);
    }

    public RefreshTokenMaterial CreateRefreshToken()
    {
        var bytes = RandomNumberGenerator.GetBytes(32);
        var plain = Base64UrlEncoder.Encode(bytes);
        var expires = DateTimeOffset.UtcNow.AddDays(_options.RefreshTokenDays);
        return new RefreshTokenMaterial(plain, HashRefreshToken(plain), expires);
    }

    public string HashRefreshToken(string plainValue)
    {
        var hash = SHA256.HashData(Encoding.UTF8.GetBytes(plainValue));
        return Convert.ToHexString(hash);
    }
}
