using AuthService.Domain.Entities;

namespace AuthService.Application.DTOs;

public class RefreshTokenRequest{
    public string RefreshToken {get; set;} = string.Empty;
}