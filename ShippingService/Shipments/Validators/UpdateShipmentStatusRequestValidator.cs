using FluentValidation;

namespace ShippingService.Shipments.Validators;

public class UpdateShipmentStatusRequestValidator : AbstractValidator<UpdateShipmentStatus.UpdateShipmentStatusRequest>
{
	public UpdateShipmentStatusRequestValidator()
	{
		RuleFor(x => x.Status).IsInEnum();
	}
}
