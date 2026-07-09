using FluentValidation;

namespace Ordevo.Modules.Sync.Application;

public sealed class RegisterDeviceValidator : AbstractValidator<RegisterDeviceRequest>
{
    public RegisterDeviceValidator()
    {
        RuleFor(x => x.Name).NotEmpty().MaximumLength(120);
        RuleFor(x => x.Fingerprint).NotEmpty().MaximumLength(200);
        RuleFor(x => x.DeviceType).NotEmpty().MaximumLength(30)
            .Must(x => x is "pos" or "waiter" or "kds" or "kitchen" or "manager" or "courier");
    }
}

public sealed class HeartbeatValidator : AbstractValidator<HeartbeatRequest>
{
    public HeartbeatValidator()
    {
        RuleFor(x => x.DeviceId).MaximumLength(36);
        RuleFor(x => x.LocalStoreId).MaximumLength(80);
        RuleFor(x => x.AppVersion).MaximumLength(80);
    }
}

public sealed class AckPullValidator : AbstractValidator<AckPullRequest>
{
    public AckPullValidator() => RuleFor(x => x.LastPullVersion).GreaterThanOrEqualTo(0);
}

public sealed class PushChangesValidator : AbstractValidator<PushChangesRequest>
{
    public PushChangesValidator()
    {
        RuleFor(x => x.Mutations).NotNull().Must(x => x.Count <= 500);
        RuleForEach(x => x.Mutations).SetValidator(new ClientMutationValidator());
    }
}

public sealed class ClientMutationValidator : AbstractValidator<ClientMutationRequest>
{
    public ClientMutationValidator()
    {
        RuleFor(x => x.ClientMutationId).NotEmpty().MaximumLength(100);
        RuleFor(x => x.EntityName).NotEmpty().MaximumLength(80);
        RuleFor(x => x.EntityId).NotEmpty().MaximumLength(100);
        RuleFor(x => x.Operation).Must(x => x is "upsert" or "delete" or "custom");
        RuleFor(x => x.Payload).Must(BeJsonOrEmpty).WithMessage("Payload must be JSON when provided.");
    }

    private static bool BeJsonOrEmpty(string? payload)
    {
        if (string.IsNullOrWhiteSpace(payload)) return true;
        var trimmed = payload.Trim();
        return (trimmed.StartsWith("{") && trimmed.EndsWith("}")) ||
               (trimmed.StartsWith("[") && trimmed.EndsWith("]"));
    }
}

public sealed class AppendChangeValidator : AbstractValidator<AppendChangeRequest>
{
    public AppendChangeValidator()
    {
        RuleFor(x => x.EntityName).NotEmpty().MaximumLength(80);
        RuleFor(x => x.EntityId).NotEmpty().MaximumLength(100);
        RuleFor(x => x.Operation).Must(x => x is "upsert" or "delete" or "snapshot" or "custom");
        RuleFor(x => x.Payload).Must(ClientMutationValidatorBeJsonOrEmpty).WithMessage("Payload must be JSON when provided.");
    }

    private static bool ClientMutationValidatorBeJsonOrEmpty(string? payload)
    {
        if (string.IsNullOrWhiteSpace(payload)) return true;
        var trimmed = payload.Trim();
        return (trimmed.StartsWith("{") && trimmed.EndsWith("}")) ||
               (trimmed.StartsWith("[") && trimmed.EndsWith("]"));
    }
}
