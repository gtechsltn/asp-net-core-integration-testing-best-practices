using FluentValidation;
using ShippingService.Shared.Models;

namespace ShippingService.Shipments.Validators;

public class AddressValidator : AbstractValidator<Address>
{
	public AddressValidator()
	{
		RuleFor(address => address.Street)
			.NotEmpty()
			.WithMessage("Street must not be empty");

		RuleFor(address => address.City)
			.NotEmpty()
			.WithMessage("City must not be empty");

		RuleFor(address => address.Zip)
			.NotEmpty()
			.WithMessage("Zip code must not be empty");
	}
}
