using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Ordevo.Modules.Identity.Application;
using Ordevo.Modules.Identity.Domain;

namespace Ordevo.Modules.Identity.Infrastructure;

public sealed class IdentitySeeder(
    ITenantRepository tenants,
    IBranchRepository branches,
    IRoleRepository roles,
    IUserRepository users,
    IPasswordHasher passwordHasher,
    IOptions<IdentityOptions> options,
    ILogger<IdentitySeeder> logger)
{
    private readonly IdentityOptions _options = options.Value;

    public async Task SeedAsync(CancellationToken ct = default)
    {
        await EnsurePermissionCatalogueAsync(ct);

        if (!_options.SeedOnStartup)
            return;

        var boot = _options.Bootstrap;

        var tenant = await tenants.GetBySlugAsync(boot.TenantSlug, ct);
        if (tenant is null)
        {
            tenant = new Domain.Tenant { Id = Guid.NewGuid().ToString(), Name = boot.TenantName, Slug = boot.TenantSlug, IsActive = true };
            await tenants.InsertAsync(tenant, ct);
            logger.LogInformation("Seeded tenant {Slug}", tenant.Slug);
        }

        var branchList = await branches.ListByTenantAsync(tenant.Id, ct);
        if (branchList.Count == 0)
        {
            var branch = new Domain.Branch
            {
                Id = Guid.NewGuid().ToString(), TenantId = tenant.Id,
                Name = boot.BranchName, Code = boot.BranchCode, IsActive = true
            };
            await branches.InsertAsync(branch, ct);
            branchList = [branch];
            logger.LogInformation("Seeded branch {Code}", branch.Code);
        }

        var roleIdByName = await EnsureSystemRolesAsync(tenant.Id, ct);

        var ownerEmail = boot.OwnerEmail.Trim().ToLowerInvariant();
        if (await users.GetByEmailAsync(tenant.Id, ownerEmail, ct) is null)
        {
            var owner = new Domain.User
            {
                Id = Guid.NewGuid().ToString(), TenantId = tenant.Id,
                Email = ownerEmail, FullName = boot.OwnerFullName,
                PasswordHash = passwordHasher.Hash(boot.OwnerPassword),
                IsActive = true, MustChangePassword = false
            };
            await users.InsertAsync(owner, ct);
            await users.AssignRolesAsync(owner.Id, [roleIdByName[SystemRoles.Owner]], ct);
            await users.AssignBranchesAsync(owner.Id, branchList.Select(b => b.Id), ct);
            logger.LogInformation("Seeded owner user {Email}", owner.Email);
        }
    }

    private async Task EnsurePermissionCatalogueAsync(CancellationToken ct)
    {
        foreach (var (code, description) in Permissions.Catalogue)
        {
            await roles.UpsertPermissionAsync(
                new Domain.Permission { Id = Guid.NewGuid().ToString(), Code = code, Description = description }, ct);
        }
    }

    private async Task<Dictionary<string, string>> EnsureSystemRolesAsync(string tenantId, CancellationToken ct)
    {
        var existing = (await roles.ListByTenantAsync(tenantId, ct))
            .ToDictionary(r => r.Name, r => r.Id, StringComparer.OrdinalIgnoreCase);

        var permissionIdByCode = (await roles.ListAllPermissionsAsync(ct))
            .ToDictionary(p => p.Code, p => p.Id);

        foreach (var roleName in SystemRoles.All)
        {
            if (!existing.TryGetValue(roleName, out var roleId))
            {
                roleId = Guid.NewGuid().ToString();
                await roles.InsertAsync(new Domain.Role
                {
                    Id = roleId, TenantId = tenantId, Name = roleName,
                    Description = $"System role: {roleName}", IsSystem = true
                }, ct);
                existing[roleName] = roleId;
            }

            var grantCodes = Permissions.SystemRoleGrants[roleName];
            var grantIds = grantCodes.Where(permissionIdByCode.ContainsKey).Select(c => permissionIdByCode[c]);
            await roles.SetRolePermissionsAsync(roleId, grantIds, ct);
        }

        return existing;
    }
}

public sealed class IdentitySeederHostedService(IServiceProvider services, ILogger<IdentitySeederHostedService> logger)
    : IHostedService
{
    public async Task StartAsync(CancellationToken cancellationToken)
    {
        try
        {
            using var scope = services.CreateScope();
            var seeder = scope.ServiceProvider.GetRequiredService<IdentitySeeder>();
            await seeder.SeedAsync(cancellationToken);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Identity seeding failed");
            throw;
        }
    }

    public Task StopAsync(CancellationToken cancellationToken) => Task.CompletedTask;
}
