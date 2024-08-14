using System.Data.Common;
using MassTransit;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.TestHost;
using Npgsql;
using Respawn;
using ShippingService.IntegrationTests.Consumers;
using Testcontainers.PostgreSql;
using Testcontainers.RabbitMq;

namespace ShippingService.IntegrationTests.Configuration;

public class CustomWebApplicationFactory : WebApplicationFactory<IApiMarker>, IAsyncLifetime
{
    private readonly PostgreSqlContainer _dbContainer = new PostgreSqlBuilder()
		    .WithImage("postgres:latest")
		    .WithDatabase("test")
		    .WithUsername("admin")
		    .WithPassword("admin")
		    .Build();

    private readonly RabbitMqContainer _rabbitMqContainer = new RabbitMqBuilder()
	    .WithImage("rabbitmq:3-management")
	    .WithPortBinding(8080, true)
	    .WithUsername("guest")
	    .WithPassword("guest")
	    .Build();

    private DbConnection _dbConnection = null!;
    private Respawner _respawner = null!;

    public HttpClient HttpClient { get; private set; } = null!;

    public async Task InitializeAsync()
    {
	    await _dbContainer.StartAsync();
	    await _rabbitMqContainer.StartAsync();

	    _dbConnection = new NpgsqlConnection(_dbContainer.GetConnectionString());

	    HttpClient = CreateClient();

	    await _dbConnection.OpenAsync();
	    await InitializeRespawnerAsync();
    }

    public new async Task DisposeAsync()
    {
        await _dbContainer.DisposeAsync();
        await _rabbitMqContainer.DisposeAsync();
        await _dbConnection.DisposeAsync();
    }

    public async Task ResetDatabaseAsync()
    {
	    await _respawner.ResetAsync(_dbConnection);
    }

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        Environment.SetEnvironmentVariable("ConnectionStrings:Postgres", _dbContainer.GetConnectionString());
        Environment.SetEnvironmentVariable("RabbitMQConfiguration:Host", _rabbitMqContainer.GetConnectionString());

        builder.ConfigureTestServices(services =>
        {
	        services.AddMassTransitTestHarness(x =>
	        {
		        x.AddConsumer<ShipmentCreatedConsumer>();
		        x.AddConsumer<ShipmentStatusUpdatedConsumer>();

		        x.UsingRabbitMq((context, cfg) =>
		        {
			        cfg.Host(new Uri(_rabbitMqContainer.GetConnectionString()), h =>
			        {
				        h.Username("guest");
				        h.Password("guest");
			        });

			        cfg.ConfigureEndpoints(context);
		        });
	        });
        });
    }

    private async Task InitializeRespawnerAsync()
    {
	    _respawner = await Respawner.CreateAsync(_dbConnection, new RespawnerOptions
	    {
		    SchemasToInclude = [ "shipping" ],
		    DbAdapter = DbAdapter.Postgres
	    });
    }
}
