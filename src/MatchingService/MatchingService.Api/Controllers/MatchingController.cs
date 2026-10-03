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

    // =====================================================
    // CREATE
    // =====================================================

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
        catch (Exception ex)
        {
            return BadRequest(new
            {
                message = ex.Message
            });
        }
    }

    // =====================================================
    // GET BY ID
    // =====================================================

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

    // =====================================================
    // GET BY TRIP ID
    // =====================================================

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
                message = "Matching request for this trip was not found."
            });
        }

        return Ok(result);
    }

    // =====================================================
    // START SEARCHING
    // =====================================================

    [HttpPost("{id:guid}/search")]
    public async Task<IActionResult> StartSearching(
        Guid id,
        CancellationToken cancellationToken)
    {
        try
        {
            var result =
                await _matchingService.StartSearchingAsync(
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
        catch (Exception ex)
        {
            return BadRequest(new
            {
                message = ex.Message
            });
        }
    }

    // =====================================================
    // ASSIGN DRIVER
    // =====================================================

    [HttpPost("{id:guid}/assign")]
    public async Task<IActionResult> AssignDriver(
        Guid id,
        [FromBody] AssignDriverRequest request,
        CancellationToken cancellationToken)
    {
        try
        {
            var result =
                await _matchingService.AssignDriverAsync(
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
        catch (Exception ex)
        {
            return BadRequest(new
            {
                message = ex.Message
            });
        }
    }

    // =====================================================
    // FAIL
    // =====================================================

    [HttpPost("{id:guid}/fail")]
    public async Task<IActionResult> Fail(
        Guid id,
        CancellationToken cancellationToken)
    {
        try
        {
            var result =
                await _matchingService.MarkFailedAsync(
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
        catch (Exception ex)
        {
            return BadRequest(new
            {
                message = ex.Message
            });
        }
    }

    // =====================================================
    // CANCEL
    // =====================================================

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
        catch (Exception ex)
        {
            return BadRequest(new
            {
                message = ex.Message
            });
        }
    }

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
        catch (Exception ex)
        {
            return BadRequest(new
            {
                message = ex.Message
            });
        }
    }

}