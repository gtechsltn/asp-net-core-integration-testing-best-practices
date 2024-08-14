namespace ShippingService.IntegrationTests.Configuration;

[CollectionDefinition("ShippingTests")]
public class SharedTestCollection : ICollectionFixture<CustomWebApplicationFactory>;
