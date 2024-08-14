namespace ShippingService.IntegrationTests.Contracts.Responses;

public sealed record ShipmentResponse(
	string Number,
	string OrderId,
	Address Address,
	string Carrier,
	string ReceiverEmail,
	ShipmentStatus Status,
	List<ShipmentItem> Items);
