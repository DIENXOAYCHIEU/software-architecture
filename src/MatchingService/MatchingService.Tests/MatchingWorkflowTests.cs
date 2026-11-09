using MatchingService.Application.DTOs;
using MatchingService.Application.Interfaces;
using MatchingAppService = MatchingService.Application.Services.MatchingService;
using MatchingService.Domain.Entities;
using MatchingService.Domain.Enums;
using MatchingService.Domain.Exceptions;
using MatchingService.Domain.ValueObjects;
using Xunit;

namespace MatchingService.Tests;

public sealed class MatchingWorkflowTests
{
    [Fact]
    public async Task StartSearchingAsync_AssignsTheNearestAvailableDriver()
    {
        var nearDriver = new Driver("Near", new Location(-6.2001, 106.8001));
        var farDriver = new Driver("Far", new Location(-6.3000, 106.9000));
        var drivers = new InMemoryDriverRepository(nearDriver, farDriver);
        var service = CreateService(drivers, out _);

        var created = await service.CreateAsync(new CreateMatchingRequest
        {
            TripId = Guid.NewGuid(),
            PickupLatitude = -6.2,
            PickupLongitude = 106.8
        });

        var result = await service.StartSearchingAsync(created.MatchingId);

        Assert.Equal(MatchingStatus.Matched.ToString(), result.Status);
        Assert.Equal(nearDriver.Id, result.DriverId);
        Assert.Equal(DriverStatus.Busy, nearDriver.Status);
    }

    [Fact]
    public async Task StartSearchingAsync_MarksRequestFailed_WhenNoDriverIsAvailable()
    {
        var service = CreateService(new InMemoryDriverRepository(), out _);
        var created = await service.CreateAsync(new CreateMatchingRequest
        {
            TripId = Guid.NewGuid(),
            PickupLatitude = -6.2,
            PickupLongitude = 106.8
        });

        var result = await service.StartSearchingAsync(created.MatchingId);

        Assert.Equal(MatchingStatus.Failed.ToString(), result.Status);
        Assert.Null(result.DriverId);
    }

    [Fact]
    public async Task AssignDriverAsync_StartsPendingRequestBeforeAssigningDriver()
    {
        var driver = new Driver("Driver", new Location(-6.2, 106.8));
        var service = CreateService(new InMemoryDriverRepository(driver), out _);
        var created = await service.CreateAsync(new CreateMatchingRequest
        {
            TripId = Guid.NewGuid(),
            PickupLatitude = -6.2,
            PickupLongitude = 106.8
        });

        var result = await service.AssignDriverAsync(created.MatchingId, driver.Id);

        Assert.Equal(MatchingStatus.Matched.ToString(), result.Status);
        Assert.Equal(driver.Id, result.DriverId);
        Assert.Equal(DriverStatus.Busy, driver.Status);
    }

    [Fact]
    public void MatchingRequest_DoesNotAllowTerminalStateChanges()
    {
        var request = new MatchingRequest(Guid.NewGuid(), new Location(-6.2, 106.8));
        request.Cancel();

        Assert.Throws<InvalidMatchingException>(request.MarkFailed);
        Assert.Throws<InvalidMatchingException>(request.Cancel);
    }

    private static MatchingAppService CreateService(
        InMemoryDriverRepository drivers,
        out InMemoryUnitOfWork unitOfWork)
    {
        unitOfWork = new InMemoryUnitOfWork();
        return new MatchingAppService(
            new InMemoryMatchingRepository(),
            drivers,
            unitOfWork);
    }

    private sealed class InMemoryMatchingRepository : IMatchingRepository
    {
        private readonly Dictionary<Guid, MatchingRequest> _requests = [];

        public Task AddAsync(MatchingRequest matchingRequest, CancellationToken cancellationToken = default)
        {
            _requests.Add(matchingRequest.Id, matchingRequest);
            return Task.CompletedTask;
        }

        public Task<MatchingRequest?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default) =>
            Task.FromResult(_requests.GetValueOrDefault(id));

        public Task<MatchingRequest?> GetByTripIdAsync(Guid tripId, CancellationToken cancellationToken = default) =>
            Task.FromResult(_requests.Values.SingleOrDefault(x => x.TripId == tripId));

        public Task UpdateAsync(MatchingRequest matchingRequest, CancellationToken cancellationToken = default) =>
            Task.CompletedTask;
    }

    private sealed class InMemoryDriverRepository : IDriverRepository
    {
        private readonly Dictionary<Guid, Driver> _drivers;

        public InMemoryDriverRepository(params Driver[] drivers)
        {
            _drivers = drivers.ToDictionary(x => x.Id);
        }

        public Task AddAsync(Driver driver, CancellationToken cancellationToken = default)
        {
            _drivers.Add(driver.Id, driver);
            return Task.CompletedTask;
        }

        public Task<Driver?> GetByIdAsync(Guid driverId, CancellationToken cancellationToken = default) =>
            Task.FromResult(_drivers.GetValueOrDefault(driverId));

        public Task<List<Driver>> GetAvailableAsync(CancellationToken cancellationToken = default) =>
            Task.FromResult(_drivers.Values
                .Where(x => x.Status == DriverStatus.Available)
                .ToList());

        public Task UpdateAsync(Driver driver, CancellationToken cancellationToken = default) =>
            Task.CompletedTask;
    }

    private sealed class InMemoryUnitOfWork : IUnitOfWork
    {
        public Task<int> SaveChangesAsync(CancellationToken cancellationToken = default) =>
            Task.FromResult(1);
    }
}
