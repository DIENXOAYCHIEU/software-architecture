using Microsoft.EntityFrameworkCore;
using AuthService.Domain.Entities;
using AuthService.Infrastructure.Persistence.Configurations;
using AuthService.Application.DTOs;

namespace AuthService.Infrastructure.Persistence;

public class AuthDbContext : DbContext{

	public DbSet<User> Users {get; set;}
	public DbSet<RefreshToken> RefreshTokens {get; set;}

	public AuthDbContext(DbContextOptions<AuthDbContext> options):base(options){}

	protected override void OnModelCreating(ModelBuilder modelBuilder) {
		modelBuilder.ApplyConfiguration(new AuthConfiguration()); 
	}
}