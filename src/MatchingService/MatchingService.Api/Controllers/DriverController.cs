using MatchingService.Application.DTOs;
using MatchingService.Application.Interfaces;
using Microsoft.AspNetCore.Mvc;

namespace MatchingService.Api.Controllers;

[ApiController]
[Route("api/drivers")]
public class DriverController : ControllerBase
{
    private readonly IDriverService _driverService;

    public DriverController(
        IDriverService driverService)
    {
        _driverService = driverService;
    }

    [HttpPost]
    public async Task<IActionResult> Create(
        [FromBody] CreateDriverRequest request,
        CancellationToken cancellationToken)
    {
        try
        {
            var result =
                await _driverService.CreateAsync(
                    request,
                    cancellationToken);

            return CreatedAtAction(
                nameof(GetById),
                new { id = result.DriverId },
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

    [HttpGet("{id:guid}")]
    public async Task<IActionResult> GetById(
        Guid id,
        CancellationToken cancellationToken)
    {
        var result =
            await _driverService.GetByIdAsync(
                id,
                cancellationToken);

        if (result == null)
        {
            return NotFound(new
            {
                message = "Driver not found."
            });
        }

        return Ok(result);
    }
}