using System.ComponentModel.DataAnnotations;

namespace Presentation.Domain;

public class UpdateModel
{
    [Required]
    public string Username { get; set; } = string.Empty;
    
    [Required]
    [EmailAddress]
    public string Email { get; set; } = string.Empty;
    
    [MinLength(6)]
    public string? Password { get; set; }
    
    [Compare("Password")]
    public string? ConfirmPassword { get; set; }
    
    [Required]
    public string FirstName { get; set; } = string.Empty;
    
    public string? MiddleName { get; set; }
    
    [Required]
    public string LastName { get; set; } = string.Empty;
    
    public string? SecondLastName { get; set; }
}