using AuthService.Domain.Entities;
using AuthService.Application.DTOs;

namespace AuthService.Application.Interfaces;

public interface IAuthService{
	Task<User?> RegisterAsync(RegisterRequest request);
	Task<User?> GetById(Guid userId);
	Task<TokenResponse?> LoginAsync(LoginRequest request);
    Task<TokenResponse?> RefreshTokenAsync(string refreshToken);
	Task<bool> RevokeRefreshTokenAsync(string refreshToken);

}