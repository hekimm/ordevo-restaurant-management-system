namespace Ordevo.Modules.Menu.Domain;

public sealed class Category
{
    public string Id { get; set; } = default!;
    public string TenantId { get; set; } = default!;
    public string? BranchId { get; set; }
    public string Name { get; set; } = default!;
    public string? Color { get; set; }
    public int SortOrder { get; set; }
    public bool IsActive { get; set; } = true;
}

public sealed class MenuItem
{
    public string Id { get; set; } = default!;
    public string TenantId { get; set; } = default!;
    public string CategoryId { get; set; } = default!;
    public string Name { get; set; } = default!;
    public string? Description { get; set; }
    public decimal Price { get; set; }
    public decimal VatRate { get; set; } = 10m;
    public string? Sku { get; set; }
    public string? ImageUrl { get; set; }
    public string? PrepStation { get; set; }
    public int SortOrder { get; set; }
    public bool IsActive { get; set; } = true;
}

public sealed class ModifierGroup
{
    public string Id { get; set; } = default!;
    public string TenantId { get; set; } = default!;
    public string Name { get; set; } = default!;
    public int MinSelect { get; set; }
    public int MaxSelect { get; set; } = 1;
    public bool IsRequired { get; set; }
}

public sealed class Modifier
{
    public string Id { get; set; } = default!;
    public string TenantId { get; set; } = default!;
    public string GroupId { get; set; } = default!;
    public string Name { get; set; } = default!;
    public decimal PriceDelta { get; set; }
    public int SortOrder { get; set; }
    public bool IsActive { get; set; } = true;
}
