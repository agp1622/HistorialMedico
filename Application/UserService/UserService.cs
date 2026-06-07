using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using System.Text.RegularExpressions;
using Core.Entities;
using Infrastructure.Context;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;
using Presentation.Domain;
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

            // Create new user, inheriting the inviting admin's tenant — a clinic admin
            // can only ever invite teammates into their own clinic, never another one.
            var newUser = new User
            {
                UserName = registerModel.Username,
                Email = registerModel.Email,
                FirstName = registerModel.FirstName,
                LastName = registerModel.LastName,
                MiddleName = registerModel.MiddleName,
                SecondLastName = registerModel.SecondLastName,
                EmailConfirmed = true, // Auto-confirm for admin created users
                TenantId = currentUserEntity.TenantId
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

            string? tenantName = null;
            if (user.TenantId.HasValue)
            {
                tenantName = await _context.Tenants
                    .Where(t => t.Id == user.TenantId.Value)
                    .Select(t => t.Name)
                    .FirstOrDefaultAsync();
            }

            _logger.LogInformation("User {Username} logged in successfully", loginModel.Username);

            return new LoginResponse
            {
                Token = token,
                Username = user.UserName ?? "",
                Email = user.Email ?? "",
                FullName = user.FullName,
                Roles = roles.ToList(),
                ExpiresAt = DateTime.UtcNow.AddHours(_settings.ExpirationHours),
                TenantId = user.TenantId,
                TenantName = tenantName
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error during login for user {Username}", loginModel.Username);
            return null;
        }
    }

    /// <summary>
    /// Provisions a brand-new tenant (clinic/practice) together with its first
    /// Admin user. This is effectively the SaaS sign-up flow: every new customer
    /// starts here, since an Admin cannot exist without a clinic to administer.
    /// Requires registerModel.TenantName to be set.
    /// </summary>
    public async Task<IdentityResult> CreateAdminUserAsync(RegisterModel registerModel)
    {
        try
        {
            if (string.IsNullOrWhiteSpace(registerModel.TenantName))
            {
                return IdentityResult.Failed(new IdentityError
                {
                    Description = "El nombre de la clínica/consultorio (TenantName) es requerido para crear una cuenta nueva"
                });
            }

            // Check if user already exists
            var existingUser = await _userManager.FindByEmailAsync(registerModel.Email);
            if (existingUser != null)
            {
                return IdentityResult.Failed(new IdentityError { Description = "Este correo ya existe" });
            }

            var slug = await GenerateUniqueTenantSlugAsync(registerModel.TenantName);
            var now = DateTime.UtcNow;

            var tenant = new Tenant
            {
                Id = Guid.NewGuid(),
                Name = registerModel.TenantName.Trim(),
                Slug = slug,
                BillingStatus = "trialing",
                Plan = "solo",
                IsActive = true,
                CreatedAt = now,
                UpdatedAt = now
            };

            await _context.Tenants.AddAsync(tenant);
            await _context.SaveChangesAsync();

            var newUser = new User
            {
                UserName = registerModel.Username,
                Email = registerModel.Email,
                FirstName = registerModel.FirstName,
                LastName = registerModel.LastName,
                MiddleName = registerModel.MiddleName,
                SecondLastName = registerModel.SecondLastName,
                EmailConfirmed = true,
                TenantId = tenant.Id
            };

            var userResult = await _userManager.CreateAsync(newUser, registerModel.Password);

            if (!userResult.Succeeded)
            {
                // Roll back the tenant we just created so we don't leave an orphaned,
                // admin-less clinic behind.
                _context.Tenants.Remove(tenant);
                await _context.SaveChangesAsync();

                return userResult;
            }

            // Add to Admin role
            await _userManager.AddToRoleAsync(newUser, "Admin");

            _logger.LogInformation(
                "Admin user {Username} created successfully for new tenant {TenantName} ({TenantId})",
                registerModel.Username, tenant.Name, tenant.Id);

            return IdentityResult.Success;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error creating admin user {Username}", registerModel.Username);
            return IdentityResult.Failed(new IdentityError { Description = "Error interno del servidor" });
        }
    }

    /// <summary>
    /// Builds a URL-friendly, unique slug from the tenant's display name
    /// (e.g. "Cirugía Sureda" -> "cirugia-sureda"), appending a short numeric
    /// suffix on collision (e.g. "cirugia-sureda-2").
    /// </summary>
    private async Task<string> GenerateUniqueTenantSlugAsync(string tenantName)
    {
        var normalized = tenantName.Trim().ToLowerInvariant();

        // Strip diacritics (í -> i, ñ -> n, etc.) so slugs stay plain ASCII.
        normalized = string.Concat(normalized.Normalize(System.Text.NormalizationForm.FormD)
            .Where(c => System.Globalization.CharUnicodeInfo.GetUnicodeCategory(c) != System.Globalization.UnicodeCategory.NonSpacingMark))
            .Normalize(System.Text.NormalizationForm.FormC);

        normalized = Regex.Replace(normalized, "[^a-z0-9]+", "-").Trim('-');
        if (string.IsNullOrEmpty(normalized))
        {
            normalized = "clinica";
        }

        var slug = normalized;
        var suffix = 1;

        while (await _context.Tenants.AnyAsync(t => t.Slug == slug))
        {
            suffix++;
            slug = $"{normalized}-{suffix}";
        }

        return slug;
    }

    // REPLACE your GenerateJwtToken method in UserService with this:

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

        // Embed the tenant claim so every downstream request can be scoped to the
        // user's clinic via ICurrentTenantService -> HistorialDbContext query filters.
        // Omitted entirely for platform super-admins (TenantId == null).
        if (user.TenantId.HasValue)
        {
            claims.Add(new Claim("tenantId", user.TenantId.Value.ToString()));
        }

        // 🎯 FIXED: Add role claims using the short name that maps correctly
        foreach (var role in roles)
        {
            claims.Add(new Claim("role", role));  // Use "role" instead of ClaimTypes.Role
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

    public async Task<IdentityResult> UpdateUserAsync(string userId, UpdateModel updateModel)
    {
        try
        {
            var user = await _userManager.FindByIdAsync(userId);
            if (user == null)
            {
                return IdentityResult.Failed(new IdentityError { Description = "Usuario no encontrado" });
            }

            // Update user properties
            user.UserName = updateModel.Username; // 👈 ADD THIS LINE
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