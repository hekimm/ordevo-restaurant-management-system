using FluentValidation;

namespace Ordevo.Modules.M9Crm.Application;

public sealed class CreateCustomerValidator : AbstractValidator<CreateCustomerRequest>
{
    public CreateCustomerValidator()
    {
        RuleFor(x => x.Phone).NotEmpty().MaximumLength(40);
        RuleFor(x => x.FullName).MaximumLength(200);
        RuleFor(x => x.Email).EmailAddress().When(x => !string.IsNullOrWhiteSpace(x.Email)).MaximumLength(160);
    }
}

public sealed class UpdateCustomerValidator : AbstractValidator<UpdateCustomerRequest>
{
    public UpdateCustomerValidator()
    {
        RuleFor(x => x.FullName).MaximumLength(200);
        RuleFor(x => x.Email).EmailAddress().When(x => !string.IsNullOrWhiteSpace(x.Email)).MaximumLength(160);
    }
}

public sealed class BlockCustomerValidator : AbstractValidator<BlockCustomerRequest>
{
    public BlockCustomerValidator() => RuleFor(x => x.Reason).NotEmpty().MaximumLength(500);
}

public sealed class CreateCustomerAddressValidator : AbstractValidator<CreateCustomerAddressRequest>
{
    public CreateCustomerAddressValidator()
    {
        RuleFor(x => x.Label).NotEmpty().MaximumLength(80);
        RuleFor(x => x.AddressLine1).NotEmpty().MaximumLength(240);
        RuleFor(x => x.Latitude).InclusiveBetween(-90, 90).When(x => x.Latitude.HasValue);
        RuleFor(x => x.Longitude).InclusiveBetween(-180, 180).When(x => x.Longitude.HasValue);
    }
}

public sealed class LoyaltyPointsValidator : AbstractValidator<LoyaltyPointsRequest>
{
    public LoyaltyPointsValidator()
    {
        RuleFor(x => x.CustomerId).NotEmpty();
        RuleFor(x => x.Points).GreaterThan(0);
    }
}

public sealed class RedeemLoyaltyValidator : AbstractValidator<RedeemLoyaltyRequest>
{
    public RedeemLoyaltyValidator()
    {
        RuleFor(x => x.CustomerId).NotEmpty();
        RuleFor(x => x.OrderId).NotEmpty();
        RuleFor(x => x.Points).GreaterThan(0);
    }
}

public sealed class AdjustLoyaltyValidator : AbstractValidator<AdjustLoyaltyRequest>
{
    public AdjustLoyaltyValidator()
    {
        RuleFor(x => x.CustomerId).NotEmpty();
        RuleFor(x => x.Points).NotEqual(0);
        RuleFor(x => x.Reason).NotEmpty().MaximumLength(500);
    }
}

public sealed class CreateCampaignValidator : AbstractValidator<CreateCampaignRequest>
{
    public CreateCampaignValidator()
    {
        RuleFor(x => x.Code).NotEmpty().MaximumLength(40);
        RuleFor(x => x.Name).NotEmpty().MaximumLength(200);
        RuleFor(x => x.DiscountType).Must(x => x is "percent" or "amount");
        RuleFor(x => x.DiscountValue).GreaterThan(0);
        RuleFor(x => x.MaxDiscountAmount).GreaterThanOrEqualTo(0).When(x => x.MaxDiscountAmount.HasValue);
        RuleFor(x => x.MinOrderAmount).GreaterThanOrEqualTo(0).When(x => x.MinOrderAmount.HasValue);
        RuleFor(x => x.StartsAt).NotEmpty();
        RuleFor(x => x.EndsAt).GreaterThan(x => x.StartsAt).When(x => x.EndsAt.HasValue);
        RuleFor(x => x.Priority).InclusiveBetween(1, 100);
    }
}

public sealed class ApplyCampaignValidator : AbstractValidator<ApplyCampaignRequest>
{
    public ApplyCampaignValidator()
    {
        RuleFor(x => x.OrderId).NotEmpty();
        RuleFor(x => x.CampaignCode).NotEmpty();
    }
}

public sealed class CreateReservationValidator : AbstractValidator<CreateReservationRequest>
{
    public CreateReservationValidator()
    {
        RuleFor(x => x.CustomerName).NotEmpty().MaximumLength(200);
        RuleFor(x => x.CustomerPhone).NotEmpty().MaximumLength(40);
        RuleFor(x => x.ReservationDate).NotEmpty();
        RuleFor(x => x.ReservationTime).NotEmpty().Matches(@"^\d{2}:\d{2}$");
        RuleFor(x => x.GuestCount).InclusiveBetween(1, 200);
    }
}

public sealed class SetReservationStatusValidator : AbstractValidator<SetReservationStatusRequest>
{
    public SetReservationStatusValidator()
        => RuleFor(x => x.Status).Must(x => x is "confirmed" or "seated" or "cancelled" or "no_show");
}

public sealed class CreateDeliveryZoneValidator : AbstractValidator<CreateDeliveryZoneRequest>
{
    public CreateDeliveryZoneValidator()
    {
        RuleFor(x => x.Name).NotEmpty().MaximumLength(160);
        RuleFor(x => x.CenterLat).InclusiveBetween(-90, 90);
        RuleFor(x => x.CenterLng).InclusiveBetween(-180, 180);
        RuleFor(x => x.RadiusKm).GreaterThan(0);
        RuleFor(x => x.DeliveryFee).GreaterThanOrEqualTo(0);
        RuleFor(x => x.MinOrderAmount).GreaterThanOrEqualTo(0);
        RuleFor(x => x.EstimatedMinutes).GreaterThan(0);
    }
}

public sealed class CreateCourierValidator : AbstractValidator<CreateCourierRequest>
{
    public CreateCourierValidator()
    {
        RuleFor(x => x.FullName).NotEmpty().MaximumLength(200);
        RuleFor(x => x.Phone).NotEmpty().MaximumLength(40);
        RuleFor(x => x.VehicleType).Must(x => x is "bike" or "motorbike" or "car" or "walk");
    }
}

public sealed class SetCourierStatusValidator : AbstractValidator<SetCourierStatusRequest>
{
    public SetCourierStatusValidator()
        => RuleFor(x => x.Status).Must(x => x is "off_duty" or "available");
}

public sealed class UpdateCourierLocationValidator : AbstractValidator<UpdateCourierLocationRequest>
{
    public UpdateCourierLocationValidator()
    {
        RuleFor(x => x.Latitude).InclusiveBetween(-90, 90);
        RuleFor(x => x.Longitude).InclusiveBetween(-180, 180);
    }
}

public sealed class CreateDeliveryValidator : AbstractValidator<CreateDeliveryRequest>
{
    public CreateDeliveryValidator()
    {
        RuleFor(x => x.OrderId).NotEmpty();
        RuleFor(x => x.DeliveryAddress).NotEmpty().MaximumLength(1000);
        RuleFor(x => x.DeliveryFee).GreaterThanOrEqualTo(0);
        RuleFor(x => x.EstimatedMinutes).GreaterThan(0);
    }
}

public sealed class SetDeliveryStatusValidator : AbstractValidator<SetDeliveryStatusRequest>
{
    public SetDeliveryStatusValidator()
        => RuleFor(x => x.Status).Must(x => x is "assigned" or "picked_up" or "on_way" or "delivered" or "cancelled");
}

public sealed class RateDeliveryValidator : AbstractValidator<RateDeliveryRequest>
{
    public RateDeliveryValidator() => RuleFor(x => x.Rating).InclusiveBetween(1, 5);
}
