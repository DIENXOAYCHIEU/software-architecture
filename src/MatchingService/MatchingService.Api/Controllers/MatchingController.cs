using MatchingService.Application.DTOs;
using MatchingService.Application.Interfaces;
using Microsoft.AspNetCore.Mvc;

namespace MatchingService.Api.Controllers;

[ApiController]
[Route("api/matching")]
public class MatchingController : ControllerBase
{
    private readonly IMatchingService _matchingService;
    private readonly IPricingService _pricingService;

    public MatchingController(
        IMatchingService matchingService,
        IPricingService pricingService)
    {
        _matchingService = matchingService;
        _pricingService = pricingService;
    }

    // CREATE MATCHING REQUEST
    [HttpPost]
    public async Task<IActionResult> Create(
        [FromBody] CreateMatchingRequest request,
        CancellationToken cancellationToken)
    {
        try
        {
            var result =
                await _matchingService.CreateAsync(
                    request,
                    cancellationToken);

            return CreatedAtAction(
                nameof(GetById),
                new { id = result.MatchingId },
                result);
        }
        catch (KeyNotFoundException ex)
        {
            return NotFound(new
            {
                message = ex.Message
            });
        }
        catch (ArgumentException ex)
        {
            return BadRequest(new
            {
                message = ex.Message
            });
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(new
            {
                message = ex.Message
            });
        }
    }

    // GET BY MATCHING ID
    [HttpGet("{id:guid}")]
    public async Task<IActionResult> GetById(
        Guid id,
        CancellationToken cancellationToken)
    {
        var result =
            await _matchingService.GetByIdAsync(
                id,
                cancellationToken);

        if (result == null)
        {
            return NotFound(new
            {
                message = "Matching request not found."
            });
        }

        return Ok(result);
    }

    // GET BY TRIP ID
    [HttpGet("trip/{tripId:guid}")]
    public async Task<IActionResult> GetByTripId(
        Guid tripId,
        CancellationToken cancellationToken)
    {
        var result =
            await _matchingService.GetByTripIdAsync(
                tripId,
                cancellationToken);

        if (result == null)
        {
            return NotFound(new
            {
                message =
                    "Matching request for this trip was not found."
            });
        }

        return Ok(result);
    }

    // START MATCHING
    [HttpPost("{id:guid}/search")]
    public async Task<IActionResult> StartMatching(
        Guid id,
        CancellationToken cancellationToken)
    {
        try
        {
            var result =
                await _matchingService.StartMatchingAsync(
                    id,
                    cancellationToken);

            return Ok(result);
        }
        catch (KeyNotFoundException ex)
        {
            return NotFound(new
            {
                message = ex.Message
            });
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(new
            {
                message = ex.Message
            });
        }
    }

    // DRIVER ACCEPT
    [HttpPost("{id:guid}/accept")]
    public async Task<IActionResult> AcceptDriver(
        Guid id,
        [FromBody] AssignDriverRequest request,
        CancellationToken cancellationToken)
    {
        try
        {
            var result =
                await _matchingService.AcceptDriverAsync(
                    id,
                    request.DriverId,
                    cancellationToken);

            return Ok(result);
        }
        catch (KeyNotFoundException ex)
        {
            return NotFound(new
            {
                message = ex.Message
            });
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(new
            {
                message = ex.Message
            });
        }
    }

    // DRIVER REJECT
    [HttpPost("{id:guid}/reject")]
    public async Task<IActionResult> RejectDriver(
        Guid id,
        [FromBody] AssignDriverRequest request,
        CancellationToken cancellationToken)
    {
        try
        {
            var result =
                await _matchingService.RejectDriverAsync(
                    id,
                    request.DriverId,
                    cancellationToken);

            return Ok(result);
        }
        catch (KeyNotFoundException ex)
        {
            return NotFound(new
            {
                message = ex.Message
            });
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(new
            {
                message = ex.Message
            });
        }
    }

    // CANCEL MATCHING
    [HttpPost("{id:guid}/cancel")]
    public async Task<IActionResult> Cancel(
        Guid id,
        CancellationToken cancellationToken)
    {
        try
        {
            var result =
                await _matchingService.CancelAsync(
                    id,
                    cancellationToken);

            return Ok(result);
        }
        catch (KeyNotFoundException ex)
        {
            return NotFound(new
            {
                message = ex.Message
            });
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(new
            {
                message = ex.Message
            });
        }
    }

    // PRICING QUOTE
    [HttpPost("quote")]
    public async Task<IActionResult> Quote(
        [FromBody] PricingRequest request,
        CancellationToken cancellationToken)
    {
        try
        {
            var result =
                await _pricingService.CalculateAsync(
                    request,
                    cancellationToken);

            return Ok(result);
        }
        catch (ArgumentException ex)
        {
            return BadRequest(new
            {
                message = ex.Message
            });
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(new
            {
                message = ex.Message
            });
        }
    }
}