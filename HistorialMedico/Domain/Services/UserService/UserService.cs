using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using HistorialMedico.Data;
using HistorialMedico.Domain.Services;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;

namespace HistorialMedico.Services;

public class UserService : IUserService
{
    private readonly JwtSettings _settings;
    private readonly HistorialDbContext _context;

    public UserService(IOptions<JwtSettings> settings, HistorialDbContext context)
    {
        _settings = settings.Value;
        _context = context;
    }

    public async Task<string> CreateUser(ApplicationUser user, string password)
    {
        user.PasswordHash = BCrypt.Net.BCrypt.HashPassword(password);
        
        await _context.AddAsync(user);
        await _context.SaveChangesAsync();
        
        return user.Id;
    }

    public Task<bool> UserExists(string username)
    {
        return _context.Users.AnyAsync(u => u.UserName.Equals(username, StringComparison.OrdinalIgnoreCase));
    }

    public string GenerateJwtToken(ApplicationUser user)
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