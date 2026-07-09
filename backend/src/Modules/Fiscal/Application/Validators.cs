using FluentValidation;

namespace Ordevo.Modules.Fiscal.Application;

public sealed class FiscalPaymentValidator : AbstractValidator<FiscalPaymentRequest>
{
    public FiscalPaymentValidator()
    {
        RuleFor(x => x.Method).Must(x => x is "cash" or "card" or "meal_voucher" or "other");
        RuleFor(x => x.Amount).GreaterThan(0);
        RuleFor(x => x.Tip).GreaterThanOrEqualTo(0);
        RuleFor(x => x.Currency).NotEmpty().Length(3);
        RuleFor(x => x.TerminalId).NotEmpty().When(x => x.Method is "card" or "meal_voucher");
        RuleFor(x => x.DocumentType).Must(x => string.IsNullOrWhiteSpace(x) || x is "efatura" or "earsiv");
        RuleFor(x => x.BuyerTaxNumber).MaximumLength(20);
        RuleFor(x => x.BuyerName).MaximumLength(200);
        RuleFor(x => x.Notes).MaximumLength(500);
    }
}

public sealed class ManualCardOverrideValidator : AbstractValidator<ManualCardOverrideRequest>
{
    public ManualCardOverrideValidator()
    {
        RuleFor(x => x.Method).Must(x => x is "card" or "meal_voucher");
        RuleFor(x => x.Amount).GreaterThan(0);
        RuleFor(x => x.Tip).GreaterThanOrEqualTo(0);
        RuleFor(x => x.Reason).NotEmpty().MinimumLength(8).MaximumLength(500);
        RuleFor(x => x.Reference).MaximumLength(160);
    }
}

public sealed class TerminalTestSaleValidator : AbstractValidator<TerminalTestSaleRequest>
{
    public TerminalTestSaleValidator()
    {
        RuleFor(x => x.Amount).GreaterThan(0).LessThanOrEqualTo(1);
        RuleFor(x => x.Currency).NotEmpty().Length(3);
    }
}
