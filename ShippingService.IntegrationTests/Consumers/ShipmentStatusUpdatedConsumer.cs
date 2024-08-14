using MassTransit;
using ShippingService.PublicApi.Events;

namespace ShippingService.IntegrationTests.Consumers;

public class ShipmentStatusUpdatedConsumer : IConsumer<ShipmentStatusUpdatedEvent>
{
	public Task Consume(ConsumeContext<ShipmentStatusUpdatedEvent> context)
	{
		return Task.CompletedTask;
	}
}
