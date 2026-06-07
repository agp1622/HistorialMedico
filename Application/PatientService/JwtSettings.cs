namespace Presentation.Domain.Services;

public class JwtSettings
{
    public string SecretKey { get; set; } = string.Empty;
    public string Issuer { get; set; } = string.Empty;
    public string Audience { get; set; } = string.Empty;
    public int ExpirationHours { get; set; } = 2;
}


public class LoginResponse
{
    public string Token { get; set; } = string.Empty;
    public string Username { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string FullName { get; set; } = string.Empty;
    public List<string> Roles { get; set; } = new();
    public DateTime ExpiresAt { get; set; }

    /// <summary>The clinic/practice this user belongs to. Null for platform-level super-admins.</summary>
    public Guid? TenantId { get; set; }

    /// <summary>Display name of the tenant, e.g. "Cirugía Sureda" — handy for the UI to show without an extra call.</summary>
    public string? TenantName { get; set; }
}