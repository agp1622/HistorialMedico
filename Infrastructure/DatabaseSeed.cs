using Azure.Core;
using Core.Entities;
using Infrastructure.Context;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;

namespace Infrastructure;

public class DatabaseSeed
{
    public static void Seed(ApplicationDbContext context, HistorialDbContext dbContext)
    {
        if (!context.Users.Any())
        {
            var hash = new PasswordHasher<User>();

            // Default development tenant — every seeded user/patient below is
            // stamped with this TenantId so the global query filters
            // (HistorialDbContext.OnModelCreating) still return data for them.
            var seedNow = DateTime.UtcNow;
            var defaultTenant = new Tenant
            {
                Id = Guid.NewGuid(),
                Name = "Clínica Demo",
                Slug = "clinica-demo",
                BillingStatus = "trialing",
                Plan = "solo",
                IsActive = true,
                CreatedAt = seedNow,
                UpdatedAt = seedNow
            };
            context.Tenants.Add(defaultTenant);

            // Create Admin Role
            var adminRole = new Role
            {
                Id = Guid.NewGuid().ToString(),
                Name = "Admin",
                NormalizedName = "ADMIN"
            };

            // Create User Role
            var userRole = new Role
            {
                Id = Guid.NewGuid().ToString(),
                Name = "User",
                NormalizedName = "USER"
            };

            // Create Admin User
            var adminUser = new User
            {
                Id = Guid.NewGuid().ToString(),
                UserName = "admin",
                NormalizedUserName = "ADMIN",
                Email = "admin@historialmedicoui.com",
                NormalizedEmail = "ADMIN@HISTORIALMEDICOUI.COM",
                EmailConfirmed = true,
                FirstName = "Administrador",
                LastName = "Sistema",
                MiddleName = "del",
                SecondLastName = "Médico",
                PasswordHash = hash.HashPassword(null, "Admin123!"),
                TenantId = defaultTenant.Id
            };

            // Create Normal User (Doctor)
            var normalUser = new User
            {
                Id = Guid.NewGuid().ToString(),
                UserName = "doctor",
                NormalizedUserName = "DOCTOR",
                Email = "doctor@historialmedicoui.com",
                NormalizedEmail = "DOCTOR@HISTORIALMEDICOUI.COM",
                EmailConfirmed = true,
                FirstName = "Juan",
                LastName = "Pérez",
                MiddleName = "Carlos",
                SecondLastName = "González",
                PasswordHash = hash.HashPassword(null, "Doctor123!"),
                TenantId = defaultTenant.Id
            };

            // Keep your existing user (Pavel)
            var pavelUser = new User
            {
                Id = Guid.NewGuid().ToString(),
                UserName = "pavelarias",
                NormalizedUserName = "PAVELARIAS",
                Email = "pavelarias@gmail.com",
                NormalizedEmail = "PAVELARIAS@GMAIL.COM",
                EmailConfirmed = true,
                FirstName = "Pavel",
                LastName = "Arias",
                PasswordHash = hash.HashPassword(null, "Geraldo123?"),
                TenantId = defaultTenant.Id
            };

            // Sample Patient (your existing patient)
            var patient = new Patient
            {
                Id = Guid.NewGuid(),
                TenantId = defaultTenant.Id,
                NumeroExpediente = "EXP001",
                Nombre = "Juan Carlos Pérez López",
                Sexo = "M",
                Edad = "4 años",
                Diagnostico = "Revisión pediátrica general",
                ReferidoPor = "Dr. María González",
                FechaNacimiento = "2020-01-15T00:00:00.000Z",
                FechaConsulta = "2024-12-01T00:00:00.000Z",
                SeguroMedico = "Seguro Nacional de Salud",
                Alergias = "Ninguna conocida",
                Madre = "Ana López de Pérez",
                MadreTelefono = "+1-809-555-0101",
                MadreCorreo = "ana.lopez@email.com",
                Padre = "Carlos Pérez Martínez",
                PadreTelefono = "+1-809-555-0102",
                PadreCorreo = "carlos.perez@email.com",
                Gestacion = "38 semanas",
                Parto = "Cesárea programada",
                PesoAlNacer = "3.2",
                PesoUnidad = "kg",
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow,
                Historial = new List<MedicalHistory>
                {
                    new MedicalHistory
                    {
                        Id = Guid.NewGuid(),
                        TenantId = defaultTenant.Id,
                        Fecha = DateTime.Today,
                        Nota = "Primera consulta pediátrica. Paciente en excelente estado general.",
                        CreatedAt = DateTime.UtcNow,
                        UpdatedAt = DateTime.UtcNow
                    }
                },
                Attachments = new List<Attachment>()
            };

            // Add roles
            context.Roles.Add(adminRole);
            context.Roles.Add(userRole);

            // Add users
            context.Users.Add(adminUser);
            context.Users.Add(normalUser);
            context.Users.Add(pavelUser);

            // Add patient
            dbContext.Patients.Add(patient);

            // Assign roles to users
            context.UserRoles.Add(new IdentityUserRole<string>
            {
                RoleId = adminRole.Id,
                UserId = adminUser.Id
            });

            context.UserRoles.Add(new IdentityUserRole<string>
            {
                RoleId = userRole.Id,
                UserId = normalUser.Id
            });

            context.UserRoles.Add(new IdentityUserRole<string>
            {
                RoleId = adminRole.Id,
                UserId = pavelUser.Id
            });

            // Save changes
            context.SaveChanges();
            dbContext.SaveChanges();

            Console.WriteLine("=== SEEDED USERS ===");
            Console.WriteLine("Admin User:");
            Console.WriteLine($"  Username: admin");
            Console.WriteLine($"  Password: Admin123!");
            Console.WriteLine($"  Email: admin@historialmedicoui.com");
            Console.WriteLine($"  Role: Admin");
            Console.WriteLine();
            Console.WriteLine("Normal User:");
            Console.WriteLine($"  Username: doctor");
            Console.WriteLine($"  Password: Doctor123!");
            Console.WriteLine($"  Email: doctor@historialmedicoui.com");
            Console.WriteLine($"  Role: User");
            Console.WriteLine();
            Console.WriteLine("Pavel (Admin):");
            Console.WriteLine($"  Username: pavelarias");
            Console.WriteLine($"  Password: Geraldo123?");
            Console.WriteLine($"  Email: pavelarias@gmail.com");
            Console.WriteLine($"  Role: Admin");
            Console.WriteLine("==================");
        }
    }

    public static void Unseed(ApplicationDbContext context, HistorialDbContext dbContext)
    {
        context.Users.RemoveRange(context.Users);
        context.Roles.RemoveRange(context.Roles);
        context.UserRoles.RemoveRange(context.UserRoles);
        context.Tenants.RemoveRange(context.Tenants);

        // IMPORTANT: this runs at app startup outside any HTTP request, so
        // ICurrentTenantService.TenantId is null and HistorialDbContext's global
        // tenant query filter (entity.TenantId == currentTenant.TenantId) would
        // match nothing — silently leaving old rows behind on every restart.
        // IgnoreQueryFilters() bypasses that for this administrative wipe.
        dbContext.Patients.RemoveRange(dbContext.Patients.IgnoreQueryFilters());
        dbContext.MedicalHistories.RemoveRange(dbContext.MedicalHistories.IgnoreQueryFilters());

        context.SaveChanges();
        dbContext.SaveChanges();
    }
}