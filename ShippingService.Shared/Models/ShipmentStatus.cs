namespace ShippingService.Shared.Models;

public enum ShipmentStatus
{
	Created,
	Processing,
	Dispatched,
	InTransit,
	WaitingCustomer,
	Delivered,
	Cancelled
}
