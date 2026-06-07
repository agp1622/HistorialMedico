using Microsoft.AspNetCore.Identity;

namespace Core.Entities;

public class User : IdentityUser
{
    /// <summary>
    /// The tenant (clinic/practice) this user belongs to. Nullable so that
    /// platform-level operators (super-admins) can exist outside any tenant;
    /// every regular clinic user must have this set.
    /// </summary>
    public Guid? TenantId { get; set; }

    public string FirstName { get; set; } = string.Empty;  // Fixed typo from "FistName"
    public string? MiddleName { get; set; }
    public string LastName { get; set; } = string.Empty;
    public string? SecondLastName { get; set; }

    // Computed property for full name
    public string FullName => 
        $"{FirstName} {MiddleName} {LastName} {SecondLastName}".Trim()
            .Replace("  ", " "); // Remove extra spaces
}