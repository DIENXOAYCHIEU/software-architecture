using AuthService.Application.Interfaces;
using AuthService.Application.DTOs;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace AuthService.Api.Controller;

[ApiController]
[Route("api/[controller]")]
public class AuthController : ControllerBase{
	private readonly IAuthService _authService;

	public AuthController(IAuthService _authService){
		this._authService=_authService;
	}

	[HttpGet("{userId}")]
	public async Task<IActionResult> GetById(
		[FromRoute] Guid userId
		){
		var result = await _authService.GetById(userId);
		return Ok(result);
	}

	[HttpPost("register")]
	public async Task<IActionResult> Register(
		[FromBody] RegisterRequest request
		){
		var response = await _authService.RegisterAsync(request);
		return Ok(response);
	}

	[HttpPost("login")]
	public async Task<IActionResult> Login(
		[FromBody] LoginRequest request
		){
	    var response = await _authService.LoginAsync(request);
	    return Ok(response);
	}

	[HttpPost("refresh")]
	public async Task<IActionResult> Refresh(
		[FromBody] RefreshTokenRequest request
		){
	    var response = await _authService.RefreshTokenAsync(request.RefreshToken);
	    if (response == null)
	    {
	        return Unauthorized("Không hợp lệ hoặc token hết hạn");
	    }

	    return Ok(response);
	}

	[HttpGet("protected")]
	[Authorize]
	public IActionResult Protected()
	{
	    var username = User.Identity?.Name;
	    return Ok($"Hello, {username} This is a protected endpoint.");
	}


	[HttpPost("revoke")]
	public async Task<IActionResult> Revoke(
		[FromBody] RefreshTokenRequest request
		){
	    var success = await _authService.RevokeRefreshTokenAsync(request.RefreshToken);
	    if (!success)
	    {
	        return BadRequest("Không hợp lệ hoặc refresh token đã được thu hồi.");
	    }

	    return Ok("Đã thu hồi refresh token thành công.");
	}
}