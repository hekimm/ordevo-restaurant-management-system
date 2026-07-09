using FluentValidation;

namespace Ordevo.Modules.Payment.Application;

public sealed class AddPaymentValidator : AbstractValidator<AddPaymentRequest>
{
    public AddPaymentValidator()
    {
        RuleFor(x => x.Method).NotEmpty();
        RuleFor(x => x.Amount).GreaterThanOrEqualTo(0);
        RuleFor(x => x.Tip).GreaterThanOrEqualTo(0);
    }
}

public sealed class RefundValidator : AbstractValidator<RefundRequest>
{
    public RefundValidator() => RuleFor(x => x.Amount).GreaterThan(0);
}
