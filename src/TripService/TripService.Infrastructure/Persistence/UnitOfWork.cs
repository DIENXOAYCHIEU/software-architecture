using TripService.Application.Interfaces;

namespace TripService.Infrastructure.Persistence;

public class UnitOfWork : IUnitOfWork{

    private readonly TripDbContext _context;

    public UnitOfWork(TripDbContext context){
        _context = context;
    }

    public async Task<int> SaveChangesAsync(CancellationToken cancellationToken = default){
        return await _context.SaveChangesAsync(cancellationToken);
    }
}
