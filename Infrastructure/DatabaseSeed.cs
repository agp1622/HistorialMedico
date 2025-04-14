using Core.Entities;
using Infrastructure.Context;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;

namespace Infrastructure;

public class DatabaseSeed
{
    public static void Seed(ApplicationDbContext context)
    {
        if (!context.Users.Any()) {}

        var hash = new PasswordHasher<User>();
        
        {
            var user = new User
            {
                Id = Guid.NewGuid().ToString(),
                UserName = "pavelarias",
                NormalizedUserName = "PAVELARIAS",
                Email = "pavelarias@gmail.com",
                NormalizedEmail = "PAVELARIAS@GMAIL.COM",
                FistName = "Pavel",
                LastName = "Arias",
                PasswordHash = hash.HashPassword(null, "Geraldo123?")
            };

            var role = new Role
            {
                Id = Guid.NewGuid().ToString(), Name = "Admin", NormalizedName = "ADMIN"
            };

            var patient = new Patient
            {
                
            };
            context.Users.Add(user);
            context.Roles.Add(role);
            
            context.UserRoles.Add(new IdentityUserRole<string>
            {
                RoleId = role.Id,
                UserId = user.Id
            });
            
            context.SaveChanges();
            
        }
    }

    public static void Unseed(ApplicationDbContext context)
    {
     
        context.Users.RemoveRange(context.Users);
        context.Roles.RemoveRange(context.Roles);
        context.UserRoles.RemoveRange(context.UserRoles);
        context.SaveChanges();
    }
}