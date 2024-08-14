using Microsoft.EntityFrameworkCore;

namespace ShippingService.Database;

public class EfCoreDbContext(DbContextOptions<EfCoreDbContext> options) : DbContext(options)
{
	public DbSet<Shipment> Shipments { get; set; }

	public DbSet<ShipmentItem> ShipmentItems { get; set; }

	protected override void OnModelCreating(ModelBuilder modelBuilder)
	{
		base.OnModelCreating(modelBuilder);

		modelBuilder.HasDefaultSchema("shipping");

		modelBuilder.Entity<Shipment>(entity =>
		{
			entity.HasKey(x => x.Id);
			entity.HasIndex(x => x.Number);

			entity.Property(x => x.Number).IsRequired();
			entity.Property(x => x.OrderId).IsRequired();
			entity.Property(x => x.Carrier).IsRequired();
			entity.Property(x => x.ReceiverEmail).IsRequired();

			entity.Property(x => x.Status)
				.HasConversion<string>()
				.IsRequired();

			entity.OwnsOne(x => x.Address, ownsBuilder =>
			{
				ownsBuilder.Property(x => x.Street).IsRequired();
				ownsBuilder.Property(x => x.City).IsRequired();
				ownsBuilder.Property(x => x.Zip).IsRequired();
			});

			entity.HasMany(x => x.Items)
				.WithOne(x => x.Shipment)
				.HasForeignKey(x => x.ShipmentId);
		});

		modelBuilder.Entity<ShipmentItem>(entity =>
		{
			entity.HasKey(x => x.Id);

			entity.Property(x => x.Id).ValueGeneratedOnAdd();

			entity.Property(x => x.Product).IsRequired();
			entity.Property(x => x.Quantity).IsRequired();
		});
	}
}
