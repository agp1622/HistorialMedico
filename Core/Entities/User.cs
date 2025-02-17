using Microsoft.AspNetCore.Identity;

namespace Core.Entities;

public class User: IdentityUser
{
    public string FistName { get; set; }
    public string? MiddleName { get; set; }
    public string LastName { get; set; }
    public string? SecondLastName { get; set; }
}