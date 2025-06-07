using System.Security.Claims;
using Microsoft.AspNetCore.Identity;
using Presentation.Domain;
using User = Core.Entities.User;
using UserService_User = Core.Entities.User;

namespace Presentation.Services;

public interface IUserService
{
    Task<IdentityResult> CreateUserAsync(RegisterModel registerModel, ClaimsPrincipal currentUser); 
    public string GenerateJwtToken(UserService_User user);
}