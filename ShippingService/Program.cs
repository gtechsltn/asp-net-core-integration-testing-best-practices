using Microsoft.EntityFrameworkCore;
using ShippingService.Database;
using ShippingService.Services;
using ShippingService.Shipments;

var builder = WebApplication.CreateBuilder(args);
builder.AddHostLogging();
builder.Services.AddWebHostInfrastructure(builder.Configuration);

var app = builder.Build();

using (var scope = app.Services.CreateScope())
{
	var dbContext = scope.ServiceProvider.GetRequiredService<EfCoreDbContext>();
	await dbContext.Database.MigrateAsync();

	var seedService = scope.ServiceProvider.GetRequiredService<SeedService>();
	await seedService.SeedDataAsync();
}

if (app.Environment.IsDevelopment())
{
	app.UseSwagger();
	app.UseSwaggerUI();
}

app.MapCreateShipmentEndpoint();
app.MapUpdateShipmentStatusEndpoint();
app.MapGetShipmentByNumberEndpoint();

app.Run();
