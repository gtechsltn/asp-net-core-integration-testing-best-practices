using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using System.Text.Json.Serialization;
using FluentAssertions;
using MassTransit.Testing;
using Microsoft.AspNetCore.Mvc;
using ShippingService.IntegrationTests.Configuration;
using ShippingService.IntegrationTests.Consumers;
using ShippingService.IntegrationTests.Contracts;
using ShippingService.IntegrationTests.Contracts.Requests;
using ShippingService.IntegrationTests.Contracts.Responses;
using ShippingService.PublicApi.Events;
using Address = ShippingService.IntegrationTests.Contracts.Address;
using ShipmentItem = ShippingService.IntegrationTests.Contracts.ShipmentItem;

namespace ShippingService.IntegrationTests.Tests;

[Collection("ShippingTests")]
public class UpdateShipmentStatusTests(CustomWebApplicationFactory factory) : IAsyncLifetime
{
	private readonly JsonSerializerOptions _jsonSerializerOptions = new()
	{
		Converters = { new JsonStringEnumConverter() },
		PropertyNamingPolicy = JsonNamingPolicy.CamelCase
	};

	public Task InitializeAsync() => Task.CompletedTask;

	public async Task DisposeAsync() => await factory.ResetDatabaseAsync();

	[Fact]
	public async Task UpdateShipmentStatus_ShouldSucceed_WhenRequestIsValid()
	{
		// Arrange
		var request = new UpdateShipmentStatusRequest(ShipmentStatus.Processing);

		// Act
		var shipmentResponse = await CreateShipmentAsync();
		var shipmentNumber = shipmentResponse.Number;

		var httpResponse = await factory.HttpClient.PostAsJsonAsync($"/api/shipments/update-status/{shipmentNumber}", request);

		// Assert
		httpResponse.StatusCode.Should().Be(HttpStatusCode.NoContent);
	}

	[Fact]
	public async Task UpdateShipmentStatus_ShouldReturnNotFound_WhenShipmentIsNotFound()
	{
		// Arrange
		var request = new UpdateShipmentStatusRequest(ShipmentStatus.Processing);

		// Act
		var httpResponse = await factory.HttpClient.PostAsJsonAsync("/api/shipments/update-status/12345", request);
		var validationResult = await httpResponse.Content.ReadFromJsonAsync<ValidationProblemDetails>();

		// Assert
		httpResponse.StatusCode.Should().Be(HttpStatusCode.NotFound);
		validationResult?.Errors.Should().HaveCount(1);

		var error = validationResult?.Errors.FirstOrDefault();
		error?.Key.Should().Be("Shipment.NotFound");
		error?.Value.First().Should().Be("Shipment with number '12345' not found");
	}

	[Fact]
	public async Task UpdateShipmentStatus_ShouldReturnBadRequest_WhenRequestContainsInvalidStatus()
	{
		// Arrange
		var request = new UpdateShipmentStatusRequest(ShipmentStatus.SomeNewStatus);

		// Act
		var shipmentResponse = await CreateShipmentAsync();
		var shipmentNumber = shipmentResponse.Number;

		var httpResponse = await factory.HttpClient.PostAsJsonAsync($"/api/shipments/update-status/{shipmentNumber}", request);

		// Assert
		httpResponse.StatusCode.Should().Be(HttpStatusCode.BadRequest);
	}

	[Fact]
	public async Task UpdateShipmentStatus_ShouldProduce_ShipmentStatusUpdatedEvent_WhenRequestIsValid()
	{
		// Arrange
		var request = new UpdateShipmentStatusRequest(ShipmentStatus.Processing);

		// Act
		var massTransitHarness = factory.Services.GetTestHarness();
		await massTransitHarness.Start();

		var shipmentResponse = await CreateShipmentAsync();
		var shipmentNumber = shipmentResponse.Number;

		var httpResponse = await factory.HttpClient.PostAsJsonAsync($"/api/shipments/update-status/{shipmentNumber}", request);

		// Assert
		httpResponse.StatusCode.Should().Be(HttpStatusCode.NoContent);

		var isEventPublished = await massTransitHarness.Published.Any<ShipmentStatusUpdatedEvent>();
		isEventPublished.Should().BeTrue();

		var consumerHarness = massTransitHarness.GetConsumerHarness<ShipmentStatusUpdatedConsumer>();

		var isEventConsumed = await consumerHarness.Consumed.Any<ShipmentStatusUpdatedEvent>(x =>
		{
			var message = x.Context.Message;

			return message.ShipmentNumber == shipmentResponse.Number &&
			       message.Status == Shared.Models.ShipmentStatus.Processing;
		});

		isEventConsumed.Should().BeTrue();

		await massTransitHarness.Stop();
	}

	private async Task<ShipmentResponse> CreateShipmentAsync()
	{
		var address = new Address("Amazing st. 5", "New York", "127675");
		List<ShipmentItem> items = [ new("Samsung Electronics", 1) ];

		var request = new CreateShipmentRequest("12345", address, "Modern Shipping", "test@mail.com", items);

		var httpResponse = await factory.HttpClient.PostAsJsonAsync("/api/shipments", request);
		return (await httpResponse.Content.ReadFromJsonAsync<ShipmentResponse>(_jsonSerializerOptions))!;
	}
}
