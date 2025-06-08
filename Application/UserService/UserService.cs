using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using Infrastructure.Context;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;
using Presentation.Domain;
using Core.Entities;
using Microsoft.Extensions.Logging;
using Presentation.Domain.Services;

namespace Presentation.Services;

public class UserService : IUserService
{
    private readonly JwtSettings _settings;
    private readonly ApplicationDbContext _context;
    private readonly UserManager<User> _userManager;
    private readonly SignInManager<User> _signInManager;
    private readonly ILogger<UserService> _logger;

    public UserService(
        IOptions<JwtSettings> settings, 
        ApplicationDbContext context, 
        UserManager<User> userManager,
        SignInManager<User> signInManager,
        ILogger<UserService> logger)
    {
        _settings = settings.Value;
        _context = context;
        _userManager = userManager;
        _signInManager = signInManager;
        _logger = logger;
    }

    public async Task<IdentityResult> CreateUserAsync(RegisterModel registerModel, ClaimsPrincipal currentUser)
    {
        try
        {
            // Check if current user is admin
            var currentUserEntity = await _userManager.GetUserAsync(currentUser);
            if (currentUserEntity == null)
            {
                return IdentityResult.Failed(new IdentityError { Description = "Usuario actual no encontrado" });
            }

            var isAdmin = await _userManager.IsInRoleAsync(currentUserEntity, "Admin");
            if (!isAdmin)
            {
                return IdentityResult.Failed(new IdentityError { Description = "No está autorizado a crear usuarios" });
            }

            // Check if user already exists
            var existingUser = await _userManager.FindByEmailAsync(registerModel.Email);
            if (existingUser != null)
            {
                return IdentityResult.Failed(new IdentityError { Description = "Este correo ya existe" });
            }

            var existingUsername = await _userManager.FindByNameAsync(registerModel.Username);
            if (existingUsername != null)
            {
                return IdentityResult.Failed(new IdentityError { Description = "Este nombre de usuario ya existe" });
            }

            // Create new user
            var newUser = new User
            {
                UserName = registerModel.Username,
                Email = registerModel.Email,
                FirstName = registerModel.FirstName,
                LastName = registerModel.LastName,
                MiddleName = registerModel.MiddleName,
                SecondLastName = registerModel.SecondLastName,
                EmailConfirmed = true // Auto-confirm for admin created users
            };

            var userResult = await _userManager.CreateAsync(newUser, registerModel.Password);

            if (!userResult.Succeeded)
            {
                return userResult;
            }

            // Add to User role by default
            await _userManager.AddToRoleAsync(newUser, "User");

            _logger.LogInformation("User {Username} created successfully by admin {AdminUsername}", 
                registerModel.Username, currentUserEntity.UserName);

            return IdentityResult.Success;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error creating user {Username}", registerModel.Username);
            return IdentityResult.Failed(new IdentityError { Description = "Error interno del servidor" });
        }
    }

    public async Task<LoginResponse?> LoginAsync(LoginModel loginModel)
    {
        try
        {
            var user = await _userManager.FindByNameAsync(loginModel.Username);
            if (user == null)
            {
                _logger.LogWarning("Login attempt with invalid username: {Username}", loginModel.Username);
                return null;
            }

            var result = await _signInManager.CheckPasswordSignInAsync(user, loginModel.Password, false);
            if (!result.Succeeded)
            {
                _logger.LogWarning("Failed login attempt for user: {Username}", loginModel.Username);
                return null;
            }

            var roles = await _userManager.GetRolesAsync(user);
            var token = GenerateJwtToken(user, roles);

            _logger.LogInformation("User {Username} logged in successfully", loginModel.Username);

            return new LoginResponse
            {
                Token = token,
                Username = user.UserName ?? "",
                Email = user.Email ?? "",
                FullName = user.FullName,
                Roles = roles.ToList(),
                ExpiresAt = DateTime.UtcNow.AddHours(_settings.ExpirationHours)
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error during login for user {Username}", loginModel.Username);
            return null;
        }
    }

    public async Task<IdentityResult> CreateAdminUserAsync(RegisterModel registerModel)
    {
        try
        {
            // Check if user already exists
            var existingUser = await _userManager.FindByEmailAsync(registerModel.Email);
            if (existingUser != null)
            {
                return IdentityResult.Failed(new IdentityError { Description = "Este correo ya existe" });
            }

            var newUser = new User
            {
                UserName = registerModel.Username,
                Email = registerModel.Email,
                FirstName = registerModel.FirstName,
                LastName = registerModel.LastName,
                MiddleName = registerModel.MiddleName,
                SecondLastName = registerModel.SecondLastName,
                EmailConfirmed = true
            };

            var userResult = await _userManager.CreateAsync(newUser, registerModel.Password);

            if (!userResult.Succeeded)
            {
                return userResult;
            }

            // Add to Admin role
            await _userManager.AddToRoleAsync(newUser, "Admin");

            _logger.LogInformation("Admin user {Username} created successfully", registerModel.Username);

            return IdentityResult.Success;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error creating admin user {Username}", registerModel.Username);
            return IdentityResult.Failed(new IdentityError { Description = "Error interno del servidor" });
        }
    }

    public string GenerateJwtToken(User user, IList<string> roles)
    {
        var key = Encoding.UTF8.GetBytes(_settings.SecretKey);

        var claims = new List<Claim>
        {
            new(JwtRegisteredClaimNames.Sub, user.Id),
            new(JwtRegisteredClaimNames.UniqueName, user.UserName ?? ""),
            new(JwtRegisteredClaimNames.Email, user.Email ?? ""),
            new(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString()),
            new("fullName", user.FullName)
        };

        // Add role claims
        foreach (var role in roles)
        {
            claims.Add(new Claim(ClaimTypes.Role, role));
        }

        var tokenDescriptor = new SecurityTokenDescriptor
        {
            Subject = new ClaimsIdentity(claims),
            Expires = DateTime.UtcNow.AddHours(_settings.ExpirationHours),
            Issuer = _settings.Issuer,
            Audience = _settings.Audience,
            SigningCredentials = new SigningCredentials(new SymmetricSecurityKey(key), SecurityAlgorithms.HmacSha256Signature)
        };

        var tokenHandler = new JwtSecurityTokenHandler();
        var token = tokenHandler.CreateToken(tokenDescriptor);

        return tokenHandler.WriteToken(token);
    }

    public async Task<bool> IsAdminAsync(ClaimsPrincipal user)
    {
        var userEntity = await _userManager.GetUserAsync(user);
        if (userEntity == null) return false;

        return await _userManager.IsInRoleAsync(userEntity, "Admin");
    }

    public async Task<IEnumerable<User>> GetAllUsersAsync()
    {
        return await _userManager.Users.ToListAsync();
    }

    public async Task<User?> GetUserByIdAsync(string userId)
    {
        return await _userManager.FindByIdAsync(userId);
    }

    public async Task<IdentityResult> UpdateUserAsync(string userId, RegisterModel updateModel)
    {
        try
        {
            var user = await _userManager.FindByIdAsync(userId);
            if (user == null)
            {
                return IdentityResult.Failed(new IdentityError { Description = "Usuario no encontrado" });
            }

            // Update user properties
            user.FirstName = updateModel.FirstName;
            user.LastName = updateModel.LastName;
            user.MiddleName = updateModel.MiddleName;
            user.SecondLastName = updateModel.SecondLastName;
            user.Email = updateModel.Email;

            var result = await _userManager.UpdateAsync(user);

            if (result.Succeeded && !string.IsNullOrEmpty(updateModel.Password))
            {
                var token = await _userManager.GeneratePasswordResetTokenAsync(user);
                await _userManager.ResetPasswordAsync(user, token, updateModel.Password);
            }

            return result;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error updating user {UserId}", userId);
            return IdentityResult.Failed(new IdentityError { Description = "Error interno del servidor" });
        }
    }

    public async Task<IdentityResult> DeleteUserAsync(string userId)
    {
        try
        {
            var user = await _userManager.FindByIdAsync(userId);
            if (user == null)
            {
                return IdentityResult.Failed(new IdentityError { Description = "Usuario no encontrado" });
            }

            var result = await _userManager.DeleteAsync(user);

            if (result.Succeeded)
            {
                _logger.LogInformation("User {UserId} deleted successfully", userId);
            }

            return result;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error deleting user {UserId}", userId);
            return IdentityResult.Failed(new IdentityError { Description = "Error interno del servidor" });
        }
    }
}