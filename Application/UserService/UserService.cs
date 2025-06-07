using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using Infrastructure.Context;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;
using Presentation.Domain;
using Presentation.Domain.Services;
using User = Core.Entities.User;
using UserService_User = Core.Entities.User;

namespace Presentation.Services;

public class UserService : IUserService
{
    private readonly JwtSettings _settings;
    private readonly ApplicationDbContext _context;
    private readonly UserManager<UserService_User> _userManager;


    public UserService(IOptions<JwtSettings> settings, ApplicationDbContext context, UserManager<UserService_User> userManager)
    {
        _settings = settings.Value;
        _context = context;
        _userManager = userManager;
    }

    public async Task<IdentityResult> CreateUserAsync(RegisterModel registerModel, ClaimsPrincipal currentUser)
    {
        var user = await _userManager.GetUserAsync(currentUser);
        var isAdmin = await _userManager.IsInRoleAsync(user, "Admin");
        if (!isAdmin)
            return IdentityResult.Failed(new IdentityError
            {
                Description = "No está autorizado a crear usuarios"
            });
        
        var existingUser = _userManager.FindByEmailAsync(registerModel.Email);
        if (existingUser is null)
            return IdentityResult.Failed(new IdentityError
            {
                Description = "Este correo ya existe"
            });

        var newUser = new UserService_User
        {
            UserName = registerModel.Username, 
            Email = registerModel.Email, 
            FistName = registerModel.FirstName,
            LastName = registerModel.LastName,
            MiddleName = registerModel.MiddleName,
            SecondLastName = registerModel.SecondLastName
        };

        var userResult = await _userManager.CreateAsync(newUser, registerModel.Password);

        if (!userResult.Succeeded) return userResult;
        
        await _userManager.AddToRoleAsync(newUser, "User");
        
        return IdentityResult.Success;
    
    }

    public string GenerateJwtToken(UserService_User user)
    {
        var key = Encoding.UTF8.GetBytes(_settings.SecretKey);
        var issuer = _settings.Issuer;

        var claims = new List<Claim>
        {
            new (JwtRegisteredClaimNames.Sub, user.Id),
            new (JwtRegisteredClaimNames.UniqueName, user.UserName ?? ""),
            new (JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString())
        };

        var tokenDescriptor = new SecurityTokenDescriptor
        {
            Subject = new ClaimsIdentity(claims),
            Expires = DateTime.UtcNow.AddHours(2),
            Issuer = issuer,
            SigningCredentials = new SigningCredentials(new SymmetricSecurityKey(key), SecurityAlgorithms.HmacSha256Signature)
        };
        
        var tokenHandler = new JwtSecurityTokenHandler();
        
        var token = tokenHandler.CreateToken(tokenDescriptor);
        
        return tokenHandler.WriteToken(token);
    }
}