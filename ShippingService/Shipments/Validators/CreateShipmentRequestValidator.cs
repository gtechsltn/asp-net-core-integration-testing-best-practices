using FluentValidation;

namespace ShippingService.Shipments.Validators;

public class CreateShipmentRequestValidator
	: AbstractValidator<CreateShipment.CreateShipmentRequest>
{
	public CreateShipmentRequestValidator()
	{
		RuleFor(shipment => shipment.OrderId)
			.NotEmpty()
			.WithMessage("OrderId must not be empty");

		RuleFor(shipment => shipment.Address)
			.Cascade(CascadeMode.Stop)
			.NotNull()
			.WithMessage("Address must not be null")
			.SetValidator(new AddressValidator());

		RuleFor(shipment => shipment.Carrier)
			.NotEmpty()
			.WithMessage("Carrier must not be empty");

		RuleFor(shipment => shipment.ReceiverEmail)
			.NotEmpty()
			.WithMessage("ReceiverEmail must not be empty");

		RuleFor(shipment => shipment.Items)
			.NotEmpty()
			.WithMessage("Items list must not be empty");
	}
}
