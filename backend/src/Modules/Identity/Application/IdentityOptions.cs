namespace Ordevo.Modules.Identity.Application;

public sealed class IdentityOptions
{
    public const string SectionName = "Identity";

    public int LockThreshold { get; set; } = 5;
    public int LockMinutes { get; set; } = 15;

    public bool SeedOnStartup { get; set; } = true;

    public BootstrapTenant Bootstrap { get; set; } = new();

    public sealed class BootstrapTenant
    {
        public string TenantName { get; set; } = "Ordevo Demo";
        public string TenantSlug { get; set; } = "demo";
        public string BranchName { get; set; } = "Merkez";
        public string BranchCode { get; set; } = "MAIN";
        public string OwnerEmail { get; set; } = "owner@ordevo.local";
        public string OwnerFullName { get; set; } = "Ordevo Owner";
        public string OwnerPassword { get; set; } = "Owner_Dev_2026!";
    }
}
