using ShippingService.Shared.Models;

namespace ShippingService.PublicApi.Events;

public record ShipmentStatusUpdatedEvent(string ShipmentNumber, ShipmentStatus Status);
