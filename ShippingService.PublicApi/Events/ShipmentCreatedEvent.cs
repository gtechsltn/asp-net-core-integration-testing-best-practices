using ShippingService.Shared.Models;

namespace ShippingService.PublicApi.Events;

public record ShipmentCreatedEvent(
	string Number,
	string OrderId,
	Address Address,
	string Carrier,
	string ReceiverEmail,
	ShipmentStatus Status);
