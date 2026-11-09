
using Moq;
using TripService.Application.DTOs;
using TripService.Application.Interfaces;
using TripService.Application.Services;
using TripService.Domain.Entities;
using TripService.Domain.Enums;
using TripService.Domain.Exceptions;
using TripService.Domain.ValueObjects;

namespace TripService.Tests;

public class TripServiceTests
{
    private readonly Mock<ITripRepository> _tripRepository = new();
    private readonly Mock<IUnitOfWork> _unitOfWork = new();
    private readonly TripService.Application.Services.TripService _service;

    private readonly Guid _riderId =
        Guid.Parse("11111111-1111-1111-1111-111111111111");

    private readonly Guid _driverId =
        Guid.Parse("22222222-2222-2222-2222-222222222222");

    public TripServiceTests()
    {
        _unitOfWork
            .Setup(x => x.SaveChangesAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(1);

        _tripRepository
            .Setup(x => x.AddAsync(It.IsAny<Trip>()))
            .Returns(Task.CompletedTask);

        _service = new TripService.Application.Services.TripService(
            _tripRepository.Object,
            _unitOfWork.Object
        );
    }

    private Trip CreateTrip()
    {
        return new Trip(
            _riderId,
            new Location(10.7769f, 106.7009f, "Quan 1"),
            new Location(10.8231f, 106.6297f, "San bay Tan Son Nhat"),
            DateTime.UtcNow
        );
    }

    [Fact]
    public async Task CreateTripAsync_ValidRequest_CreatesTripAndSaves()
    {
        var request = new CreatedTripRequest
        {
            RiderId = _riderId,
            StartLocation = new Location(10.7769f, 106.7009f, "Quan 1"),
            EndLocation = new Location(10.8231f, 106.6297f, "San bay")
        };

        var result = await _service.CreateTripAsync(request);

        Assert.NotEqual(Guid.Empty, result.Id);
        Assert.Equal(_riderId, result.RiderId);
        Assert.Equal(TripStatus.REQUESTED, result.Status);

        _tripRepository.Verify(
            x => x.AddAsync(It.Is<Trip>(trip =>
                trip.RiderId == _riderId &&
                trip.Status == TripStatus.REQUESTED)),
            Times.Once);

        _unitOfWork.Verify(
            x => x.SaveChangesAsync(It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [Fact]
    public async Task GetByIdAsync_ExistingTrip_ReturnsTrip()
    {
        var trip = CreateTrip();

        _tripRepository
            .Setup(x => x.GetByIdAsync(trip.Id))
            .ReturnsAsync(trip);

        var result = await _service.GetByIdAsync(trip.Id);

        Assert.Same(trip, result);
        Assert.Equal(trip.Id, result.Id);
    }

    [Fact]
    public async Task GetByIdAsync_TripDoesNotExist_ThrowsInvalidTripException()
    {
        var tripId = Guid.NewGuid();

        _tripRepository
            .Setup(x => x.GetByIdAsync(tripId))
            .ReturnsAsync((Trip?)null);

        await Assert.ThrowsAsync<InvalidTripException>(
            () => _service.GetByIdAsync(tripId));
    }

    [Fact]
    public async Task AssignDriverAsync_RequestedTrip_AssignsDriver()
    {
        var trip = CreateTrip();

        _tripRepository
            .Setup(x => x.GetByIdAsync(trip.Id))
            .ReturnsAsync(trip);

        var result = await _service.AssignDriverAsync(trip.Id, _driverId);

        Assert.Equal(_driverId, trip.DriverId);
        Assert.Equal(TripStatus.ACCEPTED, trip.Status);
        Assert.Equal(trip.Id, result.Id);
        Assert.Equal(TripStatus.ACCEPTED, result.Status);

        _unitOfWork.Verify(
            x => x.SaveChangesAsync(It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [Fact]
    public async Task UpdateStatusToPicked_AcceptedTrip_ChangesStatusToPicked()
    {
        var trip = CreateTrip();
        trip.AssignDriver(_driverId);

        _tripRepository
            .Setup(x => x.GetByIdAsync(trip.Id))
            .ReturnsAsync(trip);

        var result = await _service.UpdateStatusToPicked(trip.Id);

        Assert.Equal(TripStatus.PICKED, trip.Status);
        Assert.Equal(TripStatus.PICKED, result.Status);

        _unitOfWork.Verify(
            x => x.SaveChangesAsync(It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [Fact]
    public async Task UpdateStatusToDropped_PickedTrip_ChangesStatusToDropped()
    {
        var trip = CreateTrip();
        trip.AssignDriver(_driverId);
        trip.PickRiderUp();

        _tripRepository
            .Setup(x => x.GetByIdAsync(trip.Id))
            .ReturnsAsync(trip);

        var result = await _service.UpdateStatusToDropped(trip.Id);

        Assert.Equal(TripStatus.DROPPED, trip.Status);
        Assert.Equal(TripStatus.DROPPED, result.Status);

        _unitOfWork.Verify(
            x => x.SaveChangesAsync(It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [Fact]
    public async Task AssignDriverAsync_TripDoesNotExist_ThrowsInvalidTripException()
    {
        var tripId = Guid.NewGuid();

        _tripRepository
            .Setup(x => x.GetByIdAsync(tripId))
            .ReturnsAsync((Trip?)null);

        await Assert.ThrowsAsync<InvalidTripException>(
            () => _service.AssignDriverAsync(tripId, _driverId));

        _unitOfWork.Verify(
            x => x.SaveChangesAsync(It.IsAny<CancellationToken>()),
            Times.Never);
    }
}
