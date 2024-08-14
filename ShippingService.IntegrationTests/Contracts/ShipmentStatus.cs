namespace ShippingService.IntegrationTests.Contracts;

public enum ShipmentStatus
{
	Created,
	Processing,
	Dispatched,
	InTransit,
	WaitingCustomer,
	Delivered,
	Cancelled,
	SomeNewStatus
}
