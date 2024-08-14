using System.Text.Json.Serialization;
using FluentValidation;
using MassTransit;
using Microsoft.AspNetCore.Http.Json;
using Microsoft.EntityFrameworkCore;
using Serilog;
using ShippingService;
using ShippingService.Configuration;
using ShippingService.Database;
using ShippingService.Services;

// ReSharper disable once CheckNamespace
namespace Microsoft.Extensions.DependencyInjection;

public static class HostDiExtensions
{
	public static IServiceCollection AddWebHostInfrastructure(this IServiceCollection services, IConfiguration configuration)
	{
		services.AddScoped<SeedService>();

		services
			.AddEfCore(configuration)
			.AddMessageQueue(configuration);

		services
			.AddEndpointsApiExplorer()
			.AddSwaggerGen();

		services.AddMediatR(options =>
		{
			options.RegisterServicesFromAssemblyContaining<EfCoreDbContext>();
		});

		services.Configure<JsonOptions>(opt =>
		{
			opt.SerializerOptions.Converters.Add(new JsonStringEnumConverter());
		});

		services.AddValidatorsFromAssemblyContaining<IApiMarker>();

		return services;
	}

	public static void AddHostLogging(this WebApplicationBuilder builder)
	{
		builder.Host.UseSerilog((context, loggerConfig) => loggerConfig.ReadFrom.Configuration(context.Configuration));
	}

	private static IServiceCollection AddEfCore(this IServiceCollection services, IConfiguration configuration)
	{
		var postgresConnectionString = configuration.GetConnectionString("Postgres");

		services.AddDbContext<EfCoreDbContext>(x => x
			.EnableSensitiveDataLogging()
			.UseNpgsql(postgresConnectionString, npgsqlOptions => npgsqlOptions.MigrationsHistoryTable("__MyMigrationsHistory", "shipping"))
			.UseSnakeCaseNamingConvention()
		);

		return services;
	}

	private static IServiceCollection AddMessageQueue(this IServiceCollection services, IConfiguration configuration)
	{
		var rabbitMqConfiguration = configuration.GetSection(nameof(RabbitMQConfiguration)).Get<RabbitMQConfiguration>()!;

		services.AddMassTransit(busConfig =>
		{
			busConfig.SetKebabCaseEndpointNameFormatter();

			busConfig.UsingRabbitMq((context, cfg) =>
			{
				cfg.Host(new Uri(rabbitMqConfiguration.Host), h =>
				{
					h.Username(rabbitMqConfiguration.Username);
					h.Password(rabbitMqConfiguration.Password);
				});

				cfg.UseMessageRetry(r => r.Exponential(10, TimeSpan.FromSeconds(5), TimeSpan.FromSeconds(60), TimeSpan.FromSeconds(5)));

				cfg.ConfigureEndpoints(context);
			});
		});

		return services;
	}
}
