namespace Web.API.Persistence.Services.AuthService;

public class JwtSettings
{
    public string Issuer { get; set; } = "LeaktesterWorkRecord";
    public string Audience { get; set; } = "LeaktesterWorkRecord.Frontend";
    public string SigningKey { get; set; } = "LeaktesterWorkRecord-Development-Jwt-Signing-Key-2026-Change-Me";
    public int ExpiresHours { get; set; } = 8;
}
