using MatchingService.Application.Interfaces;

namespace MatchingService.Infrastructure.Persistence;

public sealed class UnitOfWork : IUnitOfWork
{
    private readonly MatchingDbContext _context;

    public UnitOfWork(MatchingDbContext context)
    {
        _context = context;
    }

    public Task<int> SaveChangesAsync(
        CancellationToken cancellationToken = default) =>
        _context.SaveChangesAsync(cancellationToken);
}
