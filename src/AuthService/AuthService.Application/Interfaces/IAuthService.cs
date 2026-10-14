using AuthService.Domain.Entities;
using AuthService.Application.DTOs;

namespace AuthService.Application.Interfaces;

public interface IAuthService{
	Task<AuthResponse?> RegisterAsync(RegisterRequest request);
	Task<User?> GetById(Guid userId);
	Task<AuthResponse?> LoginAsync(LoginRequest request);
    Task<AuthResponse?> RefreshTokenAsync(string refreshToken);
	Task<bool> RevokeRefreshTokenAsync(string refreshToken);

}