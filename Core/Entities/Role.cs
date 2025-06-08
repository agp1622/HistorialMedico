using Microsoft.AspNetCore.Identity;

namespace Core.Entities;

public class Role : IdentityRole
{
    public string? Description { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}