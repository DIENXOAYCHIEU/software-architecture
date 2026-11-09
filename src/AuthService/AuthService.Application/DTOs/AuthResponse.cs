using AuthService.Domain.Entities;

namespace AuthService.Application.DTOs;

public class AuthResponse{
    public User User {get; set;} = null!;
    public RefreshToken RefreshToken {get; set;} = null!;
    public string AccessToken {get;set;} = string.Empty;
}