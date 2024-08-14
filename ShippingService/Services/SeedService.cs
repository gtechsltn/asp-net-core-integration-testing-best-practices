using Bogus;
using Microsoft.EntityFrameworkCore;
using ShippingService.Database;
using ShippingService.Shared.Models;

namespace ShippingService.Services;

public class SeedService(EfCoreDbContext context)
{
	public async Task SeedDataAsync()
	{
		if (await context.Shipments.CountAsync(_ => true) > 0)
		{
			return;
		}

		var fakeShipments = new Faker<Shipment>()
			.RuleFor(s => s.Number, f => f.Commerce.Ean8())
			.RuleFor(s => s.OrderId, f => f.Commerce.Ean13())
			.RuleFor(s => s.Address, f => new Address
			{
				Street = f.Address.StreetAddress(),
				City = f.Address.City(),
				Zip = f.Address.ZipCode()
			})
			.RuleFor(s => s.Carrier, f => f.Commerce.Department())
			.RuleFor(s => s.ReceiverEmail, _ => "TODO: SET EMAIL HERE")
			.RuleFor(s => s.Items, f =>
				[
					..Enumerable.Range(1, f.Random.Int(1, 10))
						.Select(_ => new ShipmentItem
						{
							Product = f.Commerce.Ean8(),
							Quantity = f.Random.Int(1, 5)
						})
				]
			)
			.RuleFor(s => s.Status, ShipmentStatus.Created)
			.RuleFor(s => s.CreatedAt, f => f.Date.Past().ToUniversalTime());

		var shipments = fakeShipments.Generate(10);

		context.Shipments.AddRange(shipments);
		await context.SaveChangesAsync();
	}
}
