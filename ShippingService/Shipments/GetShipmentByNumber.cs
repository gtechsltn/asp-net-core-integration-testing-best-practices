using MediatR;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using ShippingService.Database;
using ShippingService.Shared.Models;

namespace ShippingService.Shipments;

internal static class GetShipmentByNumber
{
	internal sealed record GetShipmentByNumberQuery(string ShipmentNumber)
		: IRequest<ShipmentResponse?>;

	internal sealed record ShipmentResponse(
		string Number,
		string OrderId,
		Address Address,
		string Carrier,
		string ReceiverEmail,
		ShipmentStatus Status,
		List<ShipmentItemResponse> Items);

	internal sealed record ShipmentItemResponse(string Product, int Quantity);

	internal sealed class GetShipmentByNumberHandler(EfCoreDbContext context, ILogger<GetShipmentByNumberHandler> logger)
		: IRequestHandler<GetShipmentByNumberQuery, ShipmentResponse?>
	{
		public async Task<ShipmentResponse?> Handle(GetShipmentByNumberQuery request, CancellationToken cancellationToken)
		{
			var shipment = await context.Shipments
				.Include(x => x.Items)
				.Where(s => s.Number == request.ShipmentNumber)
				.FirstOrDefaultAsync(cancellationToken);

			if (shipment is null)
			{
				logger.LogDebug("Shipment with number {ShipmentNumber} not found", request.ShipmentNumber);
				return null;
			}

			return new ShipmentResponse(
				shipment.Number,
				shipment.OrderId,
				shipment.Address,
				shipment.Carrier,
				shipment.ReceiverEmail,
				shipment.Status,
				shipment.Items
					.Select(x => new ShipmentItemResponse(x.Product, x.Quantity))
					.ToList()
			);
		}
	}

	internal static void MapGetShipmentByNumberEndpoint(this WebApplication app)
	{
		app.MapGet("/api/shipments/{shipmentNumber}", async ([FromRoute] string shipmentNumber, IMediator mediator) =>
		{
			var response = await mediator.Send(new GetShipmentByNumberQuery(shipmentNumber));
			return response is not null ? Results.Ok(response) : Results.NotFound($"Shipment with number '{shipmentNumber}' not found");
		});
	}
}
