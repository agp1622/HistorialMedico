using Microsoft.AspNetCore.Identity;

namespace Core.Entities;

public class User : IdentityUser
{
    public string FirstName { get; set; } = string.Empty;  // Fixed typo from "FistName"
    public string? MiddleName { get; set; }
    public string LastName { get; set; } = string.Empty;
    public string? SecondLastName { get; set; }
    
    // Computed property for full name
    public string FullName => 
        $"{FirstName} {MiddleName} {LastName} {SecondLastName}".Trim()
            .Replace("  ", " "); // Remove extra spaces
}