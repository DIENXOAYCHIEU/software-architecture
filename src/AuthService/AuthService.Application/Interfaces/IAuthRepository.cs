using AuthService.Domain.Entities;
using AuthService.Application.DTOs;

namespace AuthService.Application.Interfaces;

public interface IAuthRepository{
	Task CreateAsync(User user);
	bool ExistByUsername(string username);
	Task<User> GetByUsername(string username);
	void AddRefreshToken(RefreshToken refreshToken);
	Task<RefreshToken?> RefreshTokens(string refreshToken);
	Task<User?> GetByIdAsync(Guid userId);
}