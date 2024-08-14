using Bogus;
using ErrorOr;
using FluentValidation;
using MassTransit;
using MediatR;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using ShippingService.Database;
using ShippingService.Extensions;
using ShippingService.PublicApi.Events;
using ShippingService.Shared.Models;

namespace ShippingService.Shipments;

public static class CreateShipment
{
	public sealed record CreateShipmentRequest(
		string OrderId,
		Address Address,
		string Carrier,
		string ReceiverEmail,
		List<ShipmentItem> Items);

	public sealed record ShipmentResponse(
		string Number,
		string OrderId,
		Address Address,
		string Carrier,
		string ReceiverEmail,
		ShipmentStatus Status,
		List<ShipmentItemResponse> Items);

	public sealed record ShipmentItemResponse(string Product, int Quantity);

	internal sealed record CreateShipmentCommand(
		string OrderId,
		Address Address,
		string Carrier,
		string ReceiverEmail,
		List<ShipmentItem> Items)
		: IRequest<ErrorOr<ShipmentResponse>>;

	internal sealed class CreateShipmentHandler(
		EfCoreDbContext context,
		IPublishEndpoint publishEndpoint,
		ILogger<CreateShipmentHandler> logger)
		: IRequestHandler<CreateShipmentCommand, ErrorOr<ShipmentResponse>>
	{
		public async Task<ErrorOr<ShipmentResponse>> Handle(
			CreateShipmentCommand request,
			CancellationToken cancellationToken)
		{
			var shipmentAlreadyExists = await context.Shipments
				.Where(s => s.OrderId == request.OrderId)
				.AnyAsync(cancellationToken);

			if (shipmentAlreadyExists)
			{
				logger.LogInformation("Shipment for order '{OrderId}' is already created", request.OrderId);
				return Error.Conflict($"Shipment for order '{request.OrderId}' is already created");
			}

			var shipmentNumber = new Faker().Commerce.Ean8();
			var shipment = CreateShipment(request, shipmentNumber);

			context.Shipments.Add(shipment);

			var shipmentCreatedEvent = CreateShipmentCreatedEvent(shipment);
			await publishEndpoint.Publish(shipmentCreatedEvent, cancellationToken);

			await context.SaveChangesAsync(cancellationToken);

			logger.LogInformation("Created shipment: {@Shipment}", shipment);

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

		private static Shipment CreateShipment(CreateShipmentCommand request, string shipmentNumber)
		{
			return new Shipment
			{
				Number = shipmentNumber,
				OrderId = request.OrderId,
				Address = request.Address,
				Carrier = request.Carrier,
				ReceiverEmail = request.ReceiverEmail,
				Items = request.Items,
				Status = ShipmentStatus.Created,
				CreatedAt = DateTime.UtcNow,
				UpdatedAt = null
			};
		}

		private static ShipmentCreatedEvent CreateShipmentCreatedEvent(Shipment shipment)
		{
			var shipmentCreatedEvent = new ShipmentCreatedEvent(
				shipment.Number,
				shipment.OrderId,
				shipment.Address,
				shipment.Carrier,
				shipment.ReceiverEmail,
				shipment.Status);

			return shipmentCreatedEvent;
		}
	}

	internal static void MapCreateShipmentEndpoint(this WebApplication app)
	{
		app.MapPost("/api/shipments",
			async (
				[FromBody] CreateShipmentRequest request,
				IValidator<CreateShipmentRequest> validator,
				IMediator mediator) =>
			{
				var validationResult = await validator.ValidateAsync(request);
				if (!validationResult.IsValid)
				{
					return Results.ValidationProblem(validationResult.ToDictionary());
				}

				var command = new CreateShipmentCommand(
					request.OrderId,
					request.Address,
					request.Carrier,
					request.ReceiverEmail,
					request.Items);

				var response = await mediator.Send(command);
				if (response.IsError)
				{
					return response.Errors.ToProblem();
				}

				return Results.Ok(response.Value);
			});
	}
}
