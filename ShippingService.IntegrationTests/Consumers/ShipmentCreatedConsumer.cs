using MassTransit;
using ShippingService.PublicApi.Events;

namespace ShippingService.IntegrationTests.Consumers;

public class ShipmentCreatedConsumer : IConsumer<ShipmentCreatedEvent>
{
	public Task Consume(ConsumeContext<ShipmentCreatedEvent> context)
	{
		return Task.CompletedTask;
	}
}
