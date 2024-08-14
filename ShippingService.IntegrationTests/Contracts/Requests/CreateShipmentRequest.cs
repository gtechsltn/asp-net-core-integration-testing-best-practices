namespace ShippingService.IntegrationTests.Contracts.Requests;

public record CreateShipmentRequest(
	string OrderId,
	Address Address,
	string Carrier,
	string ReceiverEmail,
	List<ShipmentItem> Items);
