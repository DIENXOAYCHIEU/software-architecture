using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using TripService.Domain.Entities;

namespace TripService.Infrastructure.Persistence.Configurations;

public class TripConfiguration : IEntityTypeConfiguration<Trip>{

    public void Configure(EntityTypeBuilder<Trip> builder){

        builder.HasKey(trip => trip.Id);
        builder.Property(trip => trip.RiderId)
            .IsRequired();
        builder.Property(trip => trip.DriverId)
            .IsRequired(false);

        builder.OwnsOne(t => t.StartLocation, navigationBuilder =>{
            navigationBuilder.Property(s => s.Latitude).HasColumnName("StartLatitude");
            navigationBuilder.Property(s => s.Longitude).HasColumnName("StartLongitude");
            navigationBuilder.Property(s => s.Address).HasColumnName("StartAddress").HasMaxLength(255);
        });
        
        builder.OwnsOne(t => t.EndLocation, navigationBuilder =>{
            navigationBuilder.Property(e => e.Latitude).HasColumnName("EndLatitude");
            navigationBuilder.Property(e => e.Longitude).HasColumnName("EndLongitude");
            navigationBuilder.Property(e => e.Address).HasColumnName("EndAddress").HasMaxLength(255);
        });

        builder.Property(trip => trip.AmountToPay)
            .HasPrecision(18, 2)
            .IsRequired(false);

        builder.Property(trip => trip.Created)
            .IsRequired();

        builder.Property(trip => trip.Updated)
            .IsRequired();

        builder.Property(t => t.Status)
               .HasConversion<string>()
               .HasMaxLength(20);
        }
}