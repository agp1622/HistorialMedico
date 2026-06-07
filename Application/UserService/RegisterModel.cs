using System.ComponentModel.DataAnnotations;

namespace Presentation.Domain;

public class RegisterModel
{
    [Required]
    public string Username { get; set; } = string.Empty;
    
    [Required]
    [EmailAddress]
    public string Email { get; set; } = string.Empty;
    
    [Required]
    [MinLength(6)]
    public string Password { get; set; } = string.Empty;
    
    [Required]
    [Compare("Password")]
    public string ConfirmPassword { get; set; } = string.Empty;
    
    [Required]
    public string FirstName { get; set; } = string.Empty;
    
    public string? MiddleName { get; set; }
    
    [Required]
    public string LastName { get; set; } = string.Empty;

    public string? SecondLastName { get; set; }

    /// <summary>
    /// Name of the clinic/practice to create, e.g. "Cirugía Sureda".
    /// Required only for the "create-admin" / new-tenant signup flow
    /// (CreateAdminUserAsync), where it provisions a brand-new Tenant whose
    /// first Admin is the user being registered. Ignored when an existing
    /// admin invites a teammate via CreateUserAsync — that user simply
    /// inherits the inviting admin's TenantId.
    /// </summary>
    public string? TenantName { get; set; }
}