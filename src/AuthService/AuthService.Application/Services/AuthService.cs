using AuthService.Domain.Entities;
using AuthService.Domain.Exceptions;
using AuthService.Application.Interfaces;
using AuthService.Application.DTOs;
using BCrypt.Net;
using Microsoft.IdentityModel.Tokens;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using Microsoft.Extensions.Configuration;

namespace AuthService.Application.Services;

public class AuthService : IAuthService{
	private readonly IAuthRepository _authRepository;
	private readonly IUnitOfWork _unitOfWork;
	private readonly IConfiguration _configuration;

	public AuthService(
		IAuthRepository _authRepository,
		IUnitOfWork _unitOfWork,
		IConfiguration _configuration
		){
		this._authRepository=_authRepository;
		this._unitOfWork=_unitOfWork;
		this._configuration=_configuration;
	}

	public async Task<AuthResponse?> RegisterAsync(RegisterRequest request){
		if(_authRepository.ExistByUsername(request.Username)){
			throw new InvalidAuthException("Tên tài khoản đã tồn tại.");
		}

		var user = new User(
			request.Username,
			BCrypt.Net.BCrypt.HashPassword(request.Password)
		);

		await _authRepository.CreateAsync(user);
		await _unitOfWork.SaveChangesAsync();

		var loginRequest = new LoginRequest{
			Username=user.Username,
			Password = request.Password
		};
        var response = await this.LoginAsync(loginRequest);
        return response;
	}
	
	public async Task<User?> GetById(Guid userId){
		var user = await _authRepository.GetByIdAsync(userId);
		if (user==null){
			throw new InvalidAuthException($"Người dùng {userId} đã tồn tại");
		}
		return user;
	}

	public async Task<AuthResponse?> LoginAsync(LoginRequest request){
        var user = await _authRepository.GetByUsername(request.Username);

        if (user == null ||
            !BCrypt.Net.BCrypt.Verify(request.Password, user.Password))
        {
			throw new InvalidAuthException($"Tài khoản không tồn tại");
        }

        var accessToken = GenerateAccessToken(user);
        var refreshToken = await GenerateRefreshTokenAsync(user);

        var authResponse = new AuthResponse{
        	User=user,
        	AccessToken = accessToken,
        	RefreshToken = refreshToken
        	};
		return authResponse;
    }

	private string GenerateAccessToken(User user){
        var jwtSettings = _configuration.GetSection("Jwt");
        var key = Encoding.UTF8.GetBytes(jwtSettings["Key"]!);

        var claims = new[]
        {
            new Claim(JwtRegisteredClaimNames.Sub, user.Id.ToString()),
            new Claim(JwtRegisteredClaimNames.Name, user.Username),
            new Claim(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString()),
            new Claim(ClaimTypes.NameIdentifier, user.Id.ToString())
        };

        var creds = new SigningCredentials(
            new SymmetricSecurityKey(key),
            SecurityAlgorithms.HmacSha256);

        var token = new JwtSecurityToken(
            issuer: jwtSettings["Issuer"],
            audience: jwtSettings["Audience"],
            claims: claims,
			expires: DateTime.UtcNow.AddMinutes(double.Parse(jwtSettings["AccessTokenExpirationMinutes"]!)),
            signingCredentials: creds);

        var accessToken = new JwtSecurityTokenHandler().WriteToken(token);
        return accessToken;
    }

	private async Task<RefreshToken> GenerateRefreshTokenAsync(User user){
        var refreshToken = new RefreshToken
        {
            Token = Guid.NewGuid().ToString(),
            UserId = user.Id,
            Expires = DateTime.UtcNow.AddDays(
                double.Parse(_configuration["Jwt:RefreshTokenExpirationDays"]!))
        };

        await _authRepository.CreateRefreshTokenAsync(refreshToken);
        await _unitOfWork.SaveChangesAsync();
        return refreshToken;
    }

    public async Task<AuthResponse?> RefreshTokenAsync(string refreshToken){
        var token = await _authRepository.RefreshTokenAsync(refreshToken);
	    if (token == null || token.IsRevoked || token.Expires < DateTime.UtcNow){
	        return null;
	    }

	    var user = await _authRepository.GetByIdAsync(token.UserId);
	    if (user == null)
	    {
	        return null;
	    }

	    var newAccessToken = GenerateAccessToken(user);

	    token.IsRevoked = true;
	    var newRefreshToken = await GenerateRefreshTokenAsync(user);

	    await _unitOfWork.SaveChangesAsync();

	    var authResponse = new AuthResponse{
	    	User = user,
	    	RefreshToken = newRefreshToken,
	    	AccessToken = newAccessToken
	    	};
	    return authResponse;
	}

	public async Task<bool> RevokeRefreshTokenAsync(string refreshToken)
	{
		var token = await _authRepository.RefreshTokenAsync(refreshToken);

	    if (token == null || token.IsRevoked)
	    {
	        return false;
	    }

	    token.IsRevoked = true;
	    await _unitOfWork.SaveChangesAsync();
	    return true;
	}
}