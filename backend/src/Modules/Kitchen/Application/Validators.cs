using FluentValidation;

namespace Ordevo.Modules.Kitchen.Application;

public sealed class UpsertStationValidator : AbstractValidator<UpsertStationRequest>
{
    public UpsertStationValidator()
    {
        RuleFor(x => x.Name).NotEmpty().MaximumLength(120);
        RuleFor(x => x.Code).NotEmpty().MaximumLength(40);
    }
}

public sealed class SetItemStatusValidator : AbstractValidator<SetItemStatusRequest>
{
    public SetItemStatusValidator()
        => RuleFor(x => x.Status)
            .Must(x => x is "pending" or "in_kitchen" or "ready" or "served")
            .WithMessage("Geçersiz mutfak durumu.");
}
