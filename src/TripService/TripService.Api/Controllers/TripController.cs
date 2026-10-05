using Microsoft.AspNetCore.Mvc;
using TripService.Application.Interfaces;
using TripService.Application.DTOs;

namespace TripService.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
public class TripController : ControllerBase{
	private readonly ITripService _tripService;

	public TripController(ITripService tripService){
		_tripService = tripService;
	}

	[HttpGet("{tripId}")]
    public async Task<IActionResult> GetTripById(
    	[FromRoute] Guid tripId
    	){
    	var result = await _tripService.GetByIdAsync(tripId);
        return Ok(result);
    }

	[HttpPost]
	public async Task<IActionResult> CreateTrip(
		[FromBody]
		CreatedTripRequest request
		) {
		var result = await _tripService.CreateTripAsync(request);
		return CreatedAtAction(
			nameof(GetTripById),
			new { id = result.Id },
			result
			);
	}

	[HttpPost("{tripId}/driver-accept")]
	public async Task<IActionResult> AssignDriver(
		[FromRoute] Guid tripId,
		[FromBody] Guid driverId
		) {

		var result = await _tripService.AssignDriverAsync(tripId, driverId);
		return CreatedAtAction(
			nameof(GetTripById),
			new { id = result.Id },
			result
			);
	}

	[HttpPost("{tripId}/pickup")]
	public async Task<IActionResult> UpdateStatusToPicked(
		[FromRoute] Guid tripId
		) {

		var result = await _tripService.UpdateStatusToPicked(tripId);
		return CreatedAtAction(
			nameof(GetTripById),
			new { id = result.Id },
			result
			);
	}

	[HttpPost("{tripId}/dropoff")]
	public async Task<IActionResult> UpdateStatusToDropped(
		[FromRoute] Guid tripId
		) {

		var result = await _tripService.UpdateStatusToDropped(tripId);
		return CreatedAtAction(
			nameof(GetTripById),
			new { id = result.Id },
			result
			);
	}
}