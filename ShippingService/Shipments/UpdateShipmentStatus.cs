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

public static class UpdateShipmentStatus
{
	public sealed record UpdateShipmentStatusRequest(ShipmentStatus Status);

	internal sealed record UpdateShipmentStatusCommand(string ShipmentNumber, ShipmentStatus Status)
		: IRequest<ErrorOr<Success>>;

	internal sealed class UpdateShipmentStatusHandler(
		EfCoreDbContext context,
		IPublishEndpoint publishEndpoint,
		ILogger<UpdateShipmentStatusCommand> logger)
		: IRequestHandler<UpdateShipmentStatusCommand, ErrorOr<Success>>
	{
		public async Task<ErrorOr<Success>> Handle(UpdateShipmentStatusCommand request, CancellationToken cancellationToken)
		{
			var shipment = await context.Shipments
				.Where(x => x.Number == request.ShipmentNumber)
				.FirstOrDefaultAsync(cancellationToken: cancellationToken);

			if (shipment is null)
			{
				logger.LogDebug("Shipment with number {ShipmentNumber} not found", request.ShipmentNumber);
				return Error.NotFound("Shipment.NotFound", $"Shipment with number '{request.ShipmentNumber}' not found");
			}

			shipment.Status = request.Status;

			var shipmentCreatedEvent = new ShipmentStatusUpdatedEvent(request.ShipmentNumber, request.Status);
			await publishEndpoint.Publish(shipmentCreatedEvent, cancellationToken);

			await context.SaveChangesAsync(cancellationToken);

			logger.LogInformation("Updated state of shipment {ShipmentNumber} to {NewState}", request.ShipmentNumber, request.Status);

			return Result.Success;
		}
	}

	internal static void MapUpdateShipmentStatusEndpoint(this WebApplication app)
	{
		app.MapPost("/api/shipments/update-status/{shipmentNumber}",
			async (
				[FromRoute] string shipmentNumber,
				[FromBody] UpdateShipmentStatusRequest request,
				IValidator<UpdateShipmentStatusRequest> validator,
				IMediator mediator) =>
			{
				var validationResult = await validator.ValidateAsync(request);
				if (!validationResult.IsValid)
				{
					return Results.ValidationProblem(validationResult.ToDictionary());
				}

				var command = new UpdateShipmentStatusCommand(shipmentNumber, request.Status);

				var response = await mediator.Send(command);
				if (response.IsError)
				{
					return response.Errors.ToProblem();
				}

				return Results.NoContent();
			});
	}
}
