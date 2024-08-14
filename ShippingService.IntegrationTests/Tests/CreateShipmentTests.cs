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
public class CreateShipmentTests(CustomWebApplicationFactory factory) : IAsyncLifetime
{
	private readonly JsonSerializerOptions _jsonSerializerOptions = new()
	{
		Converters = { new JsonStringEnumConverter() },
		PropertyNamingPolicy = JsonNamingPolicy.CamelCase
	};

	public Task InitializeAsync() => Task.CompletedTask;

	public async Task DisposeAsync() => await factory.ResetDatabaseAsync();

	[Fact]
	public async Task CreateShipment_ShouldSucceed_WhenRequestIsValid()
	{
		// Arrange
		var address = new Address("Amazing st. 5", "New York", "127675");

		List<ShipmentItem> items = [ new("Samsung Electronics", 1) ];

		var request = new CreateShipmentRequest("12345", address, "Modern Shipping", "test@mail.com", items);

		// Act
		var httpResponse = await factory.HttpClient.PostAsJsonAsync("/api/shipments", request);
		var shipmentResponse = (await httpResponse.Content.ReadFromJsonAsync<ShipmentResponse>(_jsonSerializerOptions))!;

		// Assert
		httpResponse.StatusCode.Should().Be(HttpStatusCode.OK);

		var expectedResponse = new ShipmentResponse(shipmentResponse.Number, "12345", address, "Modern Shipping", "test@mail.com", ShipmentStatus.Created, items);

		 shipmentResponse
			 .Should()
			 .BeEquivalentTo(expectedResponse);
	}

	[Fact]
	public async Task CreateShipment_ShouldReturnConflict_WhenShipmentForOrderIsAlreadyCreated()
	{
		// Arrange
		var address = new Address("Amazing st. 5", "New York", "127675");

		List<ShipmentItem> items = [ new("Samsung Electronics", 1) ];

		var request = new CreateShipmentRequest("12345", address, "Modern Shipping", "test@mail.com", items);

		// Act
		await factory.HttpClient.PostAsJsonAsync("/api/shipments", request);
		var httpResponse = await factory.HttpClient.PostAsJsonAsync("/api/shipments", request);
		var validationResult = await httpResponse.Content.ReadFromJsonAsync<ValidationProblemDetails>();

		// Assert
		httpResponse.StatusCode.Should().Be(HttpStatusCode.Conflict);
		validationResult?.Errors.Should().HaveCount(1);

		var error = validationResult?.Errors.FirstOrDefault();
		error?.Key.Should().Be("Shipment for order '12345' is already created");
		error?.Value.First().Should().Be("A conflict error has occurred.");
	}

	[Fact]
	public async Task CreateShipment_ShouldReturnBadRequest_WhenRequestHasNoItems()
	{
		// Arrange
		var address = new Address("Amazing st. 5", "New York", "127675");

		var request = new CreateShipmentRequest("12345", address, "Modern Shipping", "test@mail.com", []);

		// Act
		await factory.HttpClient.PostAsJsonAsync("/api/shipments", request);
		var httpResponse = await factory.HttpClient.PostAsJsonAsync("/api/shipments", request);
		var validationResult = await httpResponse.Content.ReadFromJsonAsync<ValidationProblemDetails>();

		// Assert
		httpResponse.StatusCode.Should().Be(HttpStatusCode.BadRequest);
		validationResult?.Errors.Should().HaveCount(1);

		var error = validationResult?.Errors.FirstOrDefault();
		error?.Key.Should().Be("Items");
		error?.Value.First().Should().Be("Items list must not be empty");
	}

	[Fact]
	public async Task CreateShipment_ShouldProduce_ShipmentCreatedEvent_WhenRequestIsValid()
	{
		// Arrange
		var address = new Address("Amazing st. 5", "New York", "127675");

		List<ShipmentItem> items = [ new("Samsung Electronics", 1) ];

		var request = new CreateShipmentRequest("12345", address, "Modern Shipping", "test@mail.com", items);

		// Act
		var massTransitHarness = factory.Services.GetTestHarness();
		await massTransitHarness.Start();

		var httpResponse = await factory.HttpClient.PostAsJsonAsync("/api/shipments", request);
		var shipmentResponse = (await httpResponse.Content.ReadFromJsonAsync<ShipmentResponse>(_jsonSerializerOptions))!;

		// Assert
		httpResponse.StatusCode.Should().Be(HttpStatusCode.OK);

		var expectedResponse = new ShipmentResponse(shipmentResponse.Number, "12345", address, "Modern Shipping", "test@mail.com", ShipmentStatus.Created, items);

		shipmentResponse
			.Should()
			.BeEquivalentTo(expectedResponse);

		var isEventPublished = await massTransitHarness.Published.Any<ShipmentCreatedEvent>();
		isEventPublished.Should().BeTrue();

		var consumerHarness = massTransitHarness.GetConsumerHarness<ShipmentCreatedConsumer>();

		var isEventConsumed = await consumerHarness.Consumed.Any<ShipmentCreatedEvent>(x =>
		{
			var message = x.Context.Message;

			return message.OrderId == shipmentResponse.OrderId &&
			       message.Number == shipmentResponse.Number &&
			       message.Carrier == shipmentResponse.Carrier &&
			       message.ReceiverEmail == shipmentResponse.ReceiverEmail &&
			       message.Status == Shared.Models.ShipmentStatus.Created &&
			       message.Address.City == shipmentResponse.Address.City &&
			       message.Address.Street == shipmentResponse.Address.Street &&
			       message.Address.Zip == shipmentResponse.Address.Zip;
		});

		isEventConsumed.Should().BeTrue();

		await massTransitHarness.Stop();
	}
}
