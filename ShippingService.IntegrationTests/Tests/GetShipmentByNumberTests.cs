using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using System.Text.Json.Serialization;
using FluentAssertions;
using ShippingService.IntegrationTests.Configuration;
using ShippingService.IntegrationTests.Contracts;
using ShippingService.IntegrationTests.Contracts.Requests;
using ShippingService.IntegrationTests.Contracts.Responses;
using Address = ShippingService.IntegrationTests.Contracts.Address;
using ShipmentItem = ShippingService.IntegrationTests.Contracts.ShipmentItem;

namespace ShippingService.IntegrationTests.Tests;

[Collection("ShippingTests")]
public class GetShipmentByNumberTests(CustomWebApplicationFactory factory) : IAsyncLifetime
{
	private readonly JsonSerializerOptions _jsonSerializerOptions = new()
	{
		Converters = { new JsonStringEnumConverter() },
		PropertyNamingPolicy = JsonNamingPolicy.CamelCase
	};

	public Task InitializeAsync() => Task.CompletedTask;

	public async Task DisposeAsync() => await factory.ResetDatabaseAsync();

	[Fact]
	public async Task GetShipmentByNumber_ShouldReturnShipment_WhenShipmentExists()
	{
		// Arrange
		var address = new Address("Amazing st. 5", "New York", "127675");
		List<ShipmentItem> items = [ new("Samsung Electronics", 1) ];

		// Act
		var shipmentResponse = await CreateShipmentAsync(address, items);
		var shipmentNumber = shipmentResponse.Number;

		var httpResponse = await factory.HttpClient.GetAsync($"/api/shipments/{shipmentNumber}");
		shipmentResponse = (await httpResponse.Content.ReadFromJsonAsync<ShipmentResponse>(_jsonSerializerOptions))!;

		// Assert
		httpResponse.StatusCode.Should().Be(HttpStatusCode.OK);

		var expectedResponse = new ShipmentResponse(shipmentResponse.Number, "12345", address, "Modern Shipping", "test@mail.com", ShipmentStatus.Created, items);

		shipmentResponse
			.Should()
			.BeEquivalentTo(expectedResponse);
	}

	[Fact]
	public async Task GetShipmentByNumber_ShouldReturnNotFound_WhenShipmentIsNotFound()
	{
		// Arrange
		// Act
		var httpResponse = await factory.HttpClient.GetAsync("/api/shipments/12345");
		var message = await httpResponse.Content.ReadAsStringAsync();

		// Assert
		httpResponse.StatusCode.Should().Be(HttpStatusCode.NotFound);
		message.Should().Be("\"Shipment with number '12345' not found\"");
	}

	private async Task<ShipmentResponse> CreateShipmentAsync(Address address, List<ShipmentItem> items)
	{
		var request = new CreateShipmentRequest("12345", address, "Modern Shipping", "test@mail.com", items);

		var httpResponse = await factory.HttpClient.PostAsJsonAsync("/api/shipments", request);
		return (await httpResponse.Content.ReadFromJsonAsync<ShipmentResponse>(_jsonSerializerOptions))!;
	}
}
