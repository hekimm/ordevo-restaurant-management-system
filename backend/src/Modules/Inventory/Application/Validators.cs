using FluentValidation;

namespace Ordevo.Modules.Inventory.Application;

public sealed class CreateUnitValidator : AbstractValidator<CreateUnitRequest>
{
    public CreateUnitValidator()
    {
        RuleFor(x => x.Code).NotEmpty().MaximumLength(20);
        RuleFor(x => x.Name).NotEmpty().MaximumLength(60);
    }
}

public sealed class UpsertStockItemValidator : AbstractValidator<UpsertStockItemRequest>
{
    public UpsertStockItemValidator()
    {
        RuleFor(x => x.Name).NotEmpty().MaximumLength(200);
        RuleFor(x => x.UnitId).NotEmpty();
        RuleFor(x => x.ReorderLevel).GreaterThanOrEqualTo(0);
        RuleFor(x => x.UnitCost).GreaterThanOrEqualTo(0);
    }
}

public sealed class CreateSupplierValidator : AbstractValidator<CreateSupplierRequest>
{
    public CreateSupplierValidator() => RuleFor(x => x.Name).NotEmpty().MaximumLength(200);
}

public sealed class CreatePurchaseValidator : AbstractValidator<CreatePurchaseRequest>
{
    public CreatePurchaseValidator()
    {
        RuleFor(x => x.Lines).NotEmpty();
        RuleForEach(x => x.Lines).ChildRules(l =>
        {
            l.RuleFor(x => x.StockItemId).NotEmpty();
            l.RuleFor(x => x.Quantity).GreaterThan(0);
            l.RuleFor(x => x.UnitCost).GreaterThanOrEqualTo(0);
        });
    }
}

public sealed class RecordWastageValidator : AbstractValidator<RecordWastageRequest>
{
    public RecordWastageValidator()
    {
        RuleFor(x => x.StockItemId).NotEmpty();
        RuleFor(x => x.Quantity).GreaterThan(0);
    }
}

public sealed class SetRecipeValidator : AbstractValidator<SetRecipeRequest>
{
    public SetRecipeValidator()
    {
        RuleFor(x => x.YieldQty).GreaterThan(0);
        RuleForEach(x => x.Lines).ChildRules(l =>
        {
            l.RuleFor(x => x.StockItemId).NotEmpty();
            l.RuleFor(x => x.Quantity).GreaterThan(0);
        });
    }
}
