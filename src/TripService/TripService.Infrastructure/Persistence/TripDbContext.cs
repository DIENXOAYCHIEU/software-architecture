using Microsoft.EntityFrameworkCore;
using TripService.Domain.Entities;
using TripService.Infrastructure.Persistence.Configurations;

namespace TripService.Infrastructure.Persistence;

public class TripDbContext : DbContext{
	public TripDbContext(DbContextOptions<TripDbContext> options):base(options){}

	public DbSet<Trip> Trips {get; set;}

	protected override void OnModelCreating(ModelBuilder modelBuilder) {
		modelBuilder.ApplyConfiguration(new TripConfiguration()); 
	}
}