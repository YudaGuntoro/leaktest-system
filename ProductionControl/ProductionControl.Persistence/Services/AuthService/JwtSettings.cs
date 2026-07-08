namespace ProductionControl.Persistence.Services.AuthService;

public class JwtSettings
{
    public string Issuer { get; set; } = "ProductionControl";
    public string Audience { get; set; } = "ProductionControl.Frontend";
    public string SigningKey { get; set; } = "ProductionControl-Development-Jwt-Signing-Key-2026-Change-Me";
    public int ExpiresHours { get; set; } = 8;
}
