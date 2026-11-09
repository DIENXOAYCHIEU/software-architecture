using Microsoft.EntityFrameworkCore;
using TripService.Domain.Entities;
using TripService.Infrastructure.Persistence.Configurations;

namespace TripService.Infrastructure.Persistence;

public class TripDbContext : DbContext{
	public TripDbContext(DbContextOptions<TripDbContext> options):base(options){}

	public DbSet<Trip> Trips {get; set;}
	public DbSet<Integration.OutboxMessage> OutboxMessages {get; set;}
	public DbSet<Integration.ProcessedIntegrationMessage> ProcessedIntegrationMessages {get; set;}

	protected override void OnModelCreating(ModelBuilder modelBuilder) {
		modelBuilder.ApplyConfiguration(new TripConfiguration()); 
		modelBuilder.Entity<Integration.OutboxMessage>().HasKey(x => x.Id);
		modelBuilder.Entity<Integration.OutboxMessage>().Property(x => x.EventName).HasMaxLength(100).IsRequired();
		modelBuilder.Entity<Integration.OutboxMessage>().Property(x => x.Payload).IsRequired();
		modelBuilder.Entity<Integration.ProcessedIntegrationMessage>().HasKey(x => x.MessageId);
	}
}