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

	[HttpGet("{id}")]
    public async Task<IActionResult> GetTripById(Guid id){
    	// var result = await _tripService.GetTripById(id);
        return Ok(new { Message = $"Trip route placeholder for ID {id} called successfully." });
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
}