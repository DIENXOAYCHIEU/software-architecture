using Microsoft.EntityFrameworkCore;
using MatchingService.Domain.Entities;

namespace MatchingService.Infrastructure.Persistence;

public class MatchingDbContext : DbContext
{
    public MatchingDbContext(DbContextOptions<MatchingDbContext> options) : base(options) { }

    public DbSet<Driver> Drivers { get; set; }
    public DbSet<MatchingRequest> MatchingRequests { get; set; }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<Driver>(entity =>
        {
            entity.HasKey(e => e.Id);
            
            entity.Property(e => e.Name)
                .HasMaxLength(255) // Tránh MySQL tự tạo kiểu TEXT (chậm index)
                .IsRequired();

            entity.OwnsOne(e => e.CurrentLocation, location =>
            {
                location.Property(l => l.Latitude)
                    .HasColumnName("Latitude").HasColumnType("double")
                    .IsRequired();

                location.Property(l => l.Longitude)
                    .HasColumnName("Longitude").HasColumnType("double")
                    .IsRequired();
            });
        });

        modelBuilder.Entity<MatchingRequest>(entity =>
        {
            entity.HasKey(e => e.Id);

            entity.Property(e => e.TripId)
                .IsRequired();

            entity.HasIndex(e => e.TripId)
                .IsUnique();

            entity.Property(e => e.DriverId)
                .IsRequired(false);
            entity.Property(e => e.Status)
                .HasConversion<string>().HasMaxLength(50)
                .IsRequired();
            entity.OwnsOne(e => e.PickupLocation, location =>
            {
                location.Property(l => l.Latitude)
                    .HasColumnName("PickupLatitude").HasColumnType("double")
                    .IsRequired();

                location.Property(l => l.Longitude)
                    .HasColumnName("PickupLongitude").HasColumnType("double")
                    .IsRequired();
            });
        });
    }
}
