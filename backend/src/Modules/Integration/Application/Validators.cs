using FluentValidation;

namespace Ordevo.Modules.Integration.Application;

public sealed class CreateConnectorValidator : AbstractValidator<CreateConnectorRequest>
{
    public CreateConnectorValidator()
    {
        RuleFor(x => x.Code).NotEmpty().MaximumLength(80).Matches("^[a-zA-Z0-9_.-]+$");
        RuleFor(x => x.Name).NotEmpty().MaximumLength(200);
        RuleFor(x => x.ConnectorType).Must(x => x is "webhook" or "delivery_marketplace" or "accounting" or "payment_terminal" or "fiscal" or "loyalty" or "custom");
        RuleFor(x => x.ProviderCode).NotEmpty().MaximumLength(80);
        RuleFor(x => x.BaseUrl).MaximumLength(1000).Must(BeUrlOrEmpty).WithMessage("BaseUrl must be an absolute http(s) URL when provided.");
        RuleFor(x => x.AuthType).Must(x => x is "none" or "api_key" or "basic" or "bearer" or "oauth2" or "hmac");
        RuleFor(x => x.Settings).Must(BeJsonOrEmpty).WithMessage("Settings must be JSON when provided.");
    }

    private static bool BeUrlOrEmpty(string? value)
        => string.IsNullOrWhiteSpace(value) ||
           (Uri.TryCreate(value, UriKind.Absolute, out var uri) && uri.Scheme is "http" or "https");

    internal static bool BeJsonOrEmpty(string? payload)
    {
        if (string.IsNullOrWhiteSpace(payload)) return true;
        var trimmed = payload.Trim();
        return (trimmed.StartsWith('{') && trimmed.EndsWith('}')) ||
               (trimmed.StartsWith('[') && trimmed.EndsWith(']'));
    }
}

public sealed class SetConnectorStatusValidator : AbstractValidator<SetConnectorStatusRequest>
{
    public SetConnectorStatusValidator()
    {
        RuleFor(x => x.Status).Must(x => x is "draft" or "active" or "paused" or "error");
        RuleFor(x => x.Reason).MaximumLength(1000);
    }
}

public sealed class CreateWebhookSubscriptionValidator : AbstractValidator<CreateWebhookSubscriptionRequest>
{
    public CreateWebhookSubscriptionValidator()
    {
        RuleFor(x => x.Name).NotEmpty().MaximumLength(200);
        RuleFor(x => x.TargetUrl).NotEmpty().MaximumLength(2000).Must(BeHttpUrl);
        RuleFor(x => x.EventPattern).NotEmpty().MaximumLength(120);
        RuleFor(x => x.EventFilter).Must(CreateConnectorValidator.BeJsonOrEmpty).WithMessage("EventFilter must be JSON when provided.");
        RuleFor(x => x.Headers).Must(CreateConnectorValidator.BeJsonOrEmpty).WithMessage("Headers must be JSON when provided.");
        RuleFor(x => x.MaxAttempts).InclusiveBetween(1, 20);
        RuleFor(x => x.TimeoutSeconds).InclusiveBetween(1, 120);
    }

    private static bool BeHttpUrl(string value)
        => Uri.TryCreate(value, UriKind.Absolute, out var uri) && uri.Scheme is "http" or "https";
}

public sealed class SetWebhookStatusValidator : AbstractValidator<SetWebhookStatusRequest>
{
    public SetWebhookStatusValidator() => RuleFor(x => x.Status).Must(x => x is "active" or "paused" or "error");
}

public sealed class QueueIntegrationEventValidator : AbstractValidator<QueueIntegrationEventRequest>
{
    public QueueIntegrationEventValidator()
    {
        RuleFor(x => x.SourceModule).NotEmpty().MaximumLength(80);
        RuleFor(x => x.EventType).NotEmpty().MaximumLength(120);
        RuleFor(x => x.AggregateType).NotEmpty().MaximumLength(80);
        RuleFor(x => x.AggregateId).NotEmpty().MaximumLength(100);
        RuleFor(x => x.Payload).NotEmpty().Must(CreateConnectorValidator.BeJsonOrEmpty).WithMessage("Payload must be JSON.");
        RuleFor(x => x.CorrelationId).MaximumLength(100);
    }
}

public sealed class MarkDeliverySuccessValidator : AbstractValidator<MarkDeliverySuccessRequest>
{
    public MarkDeliverySuccessValidator()
    {
        RuleFor(x => x.StatusCode).InclusiveBetween(100, 599).When(x => x.StatusCode.HasValue);
        RuleFor(x => x.RequestHeaders).Must(CreateConnectorValidator.BeJsonOrEmpty).WithMessage("RequestHeaders must be JSON when provided.");
        RuleFor(x => x.LatencyMs).GreaterThanOrEqualTo(0).When(x => x.LatencyMs.HasValue);
    }
}

public sealed class MarkDeliveryFailureValidator : AbstractValidator<MarkDeliveryFailureRequest>
{
    public MarkDeliveryFailureValidator()
    {
        RuleFor(x => x.StatusCode).InclusiveBetween(100, 599).When(x => x.StatusCode.HasValue);
        RuleFor(x => x.RequestHeaders).Must(CreateConnectorValidator.BeJsonOrEmpty).WithMessage("RequestHeaders must be JSON when provided.");
        RuleFor(x => x.ErrorMessage).MaximumLength(1000);
        RuleFor(x => x.LatencyMs).GreaterThanOrEqualTo(0).When(x => x.LatencyMs.HasValue);
    }
}

public sealed class RegisterTerminalValidator : AbstractValidator<RegisterTerminalRequest>
{
    public RegisterTerminalValidator()
    {
        RuleFor(x => x.Name).NotEmpty().MaximumLength(160);
        RuleFor(x => x.TerminalType).Must(x => x is "payment" or "fiscal" or "kitchen_printer" or "cash_drawer" or "scale" or "custom");
        RuleFor(x => x.ProviderTerminalId).MaximumLength(120);
        RuleFor(x => x.ConnectionMode).Must(x => x is "usb" or "serial" or "ethernet" or "cloud" or "app_to_app" or "custom");
        RuleFor(x => x.IpAddress).MaximumLength(64);
        RuleFor(x => x.Port).InclusiveBetween(1, 65535).When(x => x.Port.HasValue);
        RuleFor(x => x.SerialPath).MaximumLength(120);
        RuleFor(x => x.Settings).Must(CreateConnectorValidator.BeJsonOrEmpty).WithMessage("Settings must be JSON when provided.");
    }
}

public sealed class QueueTerminalCommandValidator : AbstractValidator<QueueTerminalCommandRequest>
{
    public QueueTerminalCommandValidator()
    {
        RuleFor(x => x.CommandType).Must(x => x is "sale" or "refund" or "void" or "settlement" or "print" or "open_drawer" or "custom");
        RuleFor(x => x.Payload).NotEmpty().Must(CreateConnectorValidator.BeJsonOrEmpty).WithMessage("Payload must be JSON.");
        RuleFor(x => x.IdempotencyKey).MaximumLength(120);
    }
}

public sealed class MarkCommandSentValidator : AbstractValidator<MarkCommandSentRequest>
{
    public MarkCommandSentValidator() => RuleFor(x => x.ProviderReference).MaximumLength(160);
}

public sealed class MarkCommandCompletedValidator : AbstractValidator<MarkCommandCompletedRequest>
{
    public MarkCommandCompletedValidator()
    {
        RuleFor(x => x.ProviderReference).MaximumLength(160);
        RuleFor(x => x.ResultPayload).Must(CreateConnectorValidator.BeJsonOrEmpty).WithMessage("ResultPayload must be JSON when provided.");
    }
}

public sealed class MarkCommandFailedValidator : AbstractValidator<MarkCommandFailedRequest>
{
    public MarkCommandFailedValidator()
    {
        RuleFor(x => x.ErrorCode).MaximumLength(100);
        RuleFor(x => x.ErrorMessage).MaximumLength(1000);
        RuleFor(x => x.ResultPayload).Must(CreateConnectorValidator.BeJsonOrEmpty).WithMessage("ResultPayload must be JSON when provided.");
    }
}
