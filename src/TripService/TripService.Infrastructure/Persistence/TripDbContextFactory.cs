using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;

namespace TripService.Infrastructure.Persistence;

public sealed class TripDbContextFactory : IDesignTimeDbContextFactory<TripDbContext>
{
    public TripDbContext CreateDbContext(string[] args)
    {
        var options = new DbContextOptionsBuilder<TripDbContext>()
            .UseMySql(
                "Server=localhost;Port=3306;Database=swiftride_trips;User=root;Password=;",
                ServerVersion.Parse("8.0.0-mysql"))
            .Options;

        return new TripDbContext(options);
    }
}
