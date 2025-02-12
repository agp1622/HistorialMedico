namespace HistorialMedico.Services;

public interface IUserService
{
    Task<string> CreateUser(ApplicationUser user, string password); 
    Task<bool> UserExists(string username);
    public string GenerateJwtToken(ApplicationUser user);
}