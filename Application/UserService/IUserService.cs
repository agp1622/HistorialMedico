using System.Security.Claims;
using Microsoft.AspNetCore.Identity;
using Presentation.Domain;
using Core.Entities;
using Presentation.Domain.Services;

namespace Presentation.Services;

public interface IUserService
{
    Task<IdentityResult> CreateUserAsync(RegisterModel registerModel, ClaimsPrincipal currentUser);
    Task<LoginResponse?> LoginAsync(LoginModel loginModel);
    Task<IdentityResult> CreateAdminUserAsync(RegisterModel registerModel);
    string GenerateJwtToken(User user, IList<string> roles);
    Task<bool> IsAdminAsync(ClaimsPrincipal user);
    Task<IEnumerable<User>> GetAllUsersAsync();
    Task<User?> GetUserByIdAsync(string userId);
    Task<IdentityResult> UpdateUserAsync(string userId, RegisterModel updateModel);
    Task<IdentityResult> DeleteUserAsync(string userId);
}