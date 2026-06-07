using Core.Configurations;
using Core.Entities;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;

namespace Infrastructure.Context;

public class ApplicationDbContext : IdentityDbContext<User, Role, string>
{
    public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options)
        : base(options) { }

    public DbSet<User> Users { get; set; }
    public DbSet<Role> Roles { get; set; }

    /// <summary>
    /// Tenants live alongside Identity data (in the "AspnetUsers" database) rather than
    /// in HistorialDbContext's "HistorialMedico" database. The two are physically separate
    /// SQL Server databases (see appsettings connection strings), and EF Core cannot create
    /// foreign keys across DbContexts/databases. Domain entities (Patient, MedicalHistory,
    /// etc.) therefore reference TenantId as a plain, unconstrained Guid column — tenant
    /// existence is validated in the application layer, not enforced by the database.
    ///
    /// Deliberately NOT globally query-filtered here: user lookup during login/registration
    /// happens before any tenant can be resolved from a JWT (chicken-and-egg), so a blanket
    /// filter would break authentication. Tenant scoping for user management is applied
    /// explicitly in UserService where the acting user's TenantId is already known.
    /// </summary>
    public DbSet<Tenant> Tenants { get; set; }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        modelBuilder.ApplyConfiguration(new TenantsConfiguration());
    }
}
