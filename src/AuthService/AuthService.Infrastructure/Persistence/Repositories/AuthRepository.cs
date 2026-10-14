using AuthService.Application.Interfaces;
using AuthService.Application.DTOs;
using AuthService.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace AuthService.Infrastructure.Persistence.Repositories;

public class AuthRepository : IAuthRepository{
	private readonly AuthDbContext _context;

	public AuthRepository(AuthDbContext _context){
		this._context=_context;
	}

	public async Task<User?> GetByIdAsync(Guid userId){
		return await _context.Users.FirstOrDefaultAsync(u =>u.Id == userId);
	}

	public async Task CreateAsync(User user){
		await _context.Users.AddAsync(user);
	}

	public bool ExistByUsername(string username){
		return _context.Users.Any(u=>u.Username == username);
	}

	public async Task<User> GetByUsername(string username){
		var user = await _context.Users
			.FirstOrDefaultAsync(u => u.Username == username);

		return user;
	}

	public void AddRefreshToken(RefreshToken refreshToken){
		_context.RefreshTokens.Add(refreshToken);
	}

	public async Task<RefreshToken?> RefreshTokens(string refreshToken){
	    var token = await _context.RefreshTokens
	        .FirstOrDefaultAsync(t => t.Token == refreshToken);
        return token;
	}

}
