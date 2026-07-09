using FluentValidation;

namespace Ordevo.Modules.Menu.Application;

public sealed class UpsertCategoryValidator : AbstractValidator<UpsertCategoryRequest>
{
    public UpsertCategoryValidator()
    {
        RuleFor(x => x.Name).NotEmpty().MaximumLength(150);
        RuleFor(x => x.Color).MaximumLength(16);
        RuleFor(x => x.SortOrder).GreaterThanOrEqualTo(0);
    }
}

public sealed class UpsertMenuItemValidator : AbstractValidator<UpsertMenuItemRequest>
{
    public UpsertMenuItemValidator()
    {
        RuleFor(x => x.CategoryId).NotEmpty();
        RuleFor(x => x.Name).NotEmpty().MaximumLength(200);
        RuleFor(x => x.Price).GreaterThanOrEqualTo(0);
        RuleFor(x => x.VatRate).InclusiveBetween(0, 100);
        RuleFor(x => x.SortOrder).GreaterThanOrEqualTo(0);
    }
}

public sealed class UpsertModifierGroupValidator : AbstractValidator<UpsertModifierGroupRequest>
{
    public UpsertModifierGroupValidator()
    {
        RuleFor(x => x.Name).NotEmpty().MaximumLength(150);
        RuleFor(x => x.MinSelect).GreaterThanOrEqualTo(0);
        RuleFor(x => x.MaxSelect).GreaterThanOrEqualTo(1);
        RuleFor(x => x).Must(x => x.MaxSelect >= x.MinSelect)
            .WithMessage("MaxSelect, MinSelect'ten küçük olamaz.");
    }
}

public sealed class UpsertModifierValidator : AbstractValidator<UpsertModifierRequest>
{
    public UpsertModifierValidator()
    {
        RuleFor(x => x.Name).NotEmpty().MaximumLength(150);
        RuleFor(x => x.SortOrder).GreaterThanOrEqualTo(0);
    }
}

public sealed class AddBarcodeValidator : AbstractValidator<AddBarcodeRequest>
{
    public AddBarcodeValidator() => RuleFor(x => x.Barcode).NotEmpty().MaximumLength(64);
}

public sealed class AssignModifierGroupsValidator : AbstractValidator<AssignModifierGroupsRequest>
{
    public AssignModifierGroupsValidator() => RuleFor(x => x.GroupIds).NotNull();
}
