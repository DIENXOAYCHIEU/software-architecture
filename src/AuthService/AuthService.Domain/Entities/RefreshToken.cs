namespace AuthService.Domain.Entities;

public class RefreshToken{
    public Guid Id { get; set; } = Guid.NewGuid();
    public string Token { get; set; } = string.Empty;
    public Guid UserId { get; set; }
    public DateTime Expires { get; set; }
    public bool IsCreatedByServer { get; set; } = true;
    public bool IsRevoked { get; set; } = false;
    public bool IsUsed { get; set; } = false; 
    
    public bool IsExpired => DateTime.UtcNow >= Expires;
    public bool IsActive => !IsRevoked && !IsUsed && !IsExpired;

}
