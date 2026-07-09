using FluentValidation;

namespace Ordevo.Modules.Ordering.Application;

public sealed class UpsertSectionValidator : AbstractValidator<UpsertSectionRequest>
{
    public UpsertSectionValidator() => RuleFor(x => x.Name).NotEmpty().MaximumLength(120);
}

public sealed class UpsertTableValidator : AbstractValidator<UpsertTableRequest>
{
    public UpsertTableValidator()
    {
        RuleFor(x => x.Name).NotEmpty().MaximumLength(60);
        RuleFor(x => x.Capacity).InclusiveBetween(1, 999);
    }
}

public sealed class OpenOrderValidator : AbstractValidator<OpenOrderRequest>
{
    public OpenOrderValidator()
    {
        RuleFor(x => x.OrderType).Must(t => t is "dinein" or "takeaway" or "delivery")
            .WithMessage("Geçersiz sipariş tipi.");
        RuleFor(x => x.GuestCount).GreaterThanOrEqualTo(0);
        RuleFor(x => x.TableId).NotEmpty().When(x => x.OrderType == "dinein")
            .WithMessage("Masa siparişinde masa seçilmeli.");
    }
}

public sealed class AddItemValidator : AbstractValidator<AddItemRequest>
{
    public AddItemValidator()
    {
        RuleFor(x => x.MenuItemId).NotEmpty();
        RuleFor(x => x.Quantity).GreaterThan(0);
        RuleFor(x => x.CourseNo).GreaterThanOrEqualTo(1);
    }
}

public sealed class ApplyDiscountValidator : AbstractValidator<ApplyDiscountRequest>
{
    public ApplyDiscountValidator()
    {
        RuleFor(x => x.Type).Must(t => t is "percent" or "amount").WithMessage("İskonto tipi percent veya amount olmalı.");
        RuleFor(x => x.Value).GreaterThan(0);
        RuleFor(x => x.Value).LessThanOrEqualTo(100).When(x => x.Type == "percent").WithMessage("Yüzde iskonto 100'ü geçemez.");
    }
}
