using System.Linq.Expressions;
using System.Reflection;
using Core.Entities;
using Core.Services;
using Microsoft.EntityFrameworkCore;

namespace Infrastructure.Context;

public class HistorialDbContext: DbContext
{
    private readonly ICurrentTenantService _currentTenant;

    public HistorialDbContext(DbContextOptions<HistorialDbContext> options, ICurrentTenantService currentTenant)
        : base(options)
    {
        _currentTenant = currentTenant;
    }

    public DbSet<Patient> Patients { get; set; }
    public DbSet<ExpedienteCounter> ExpedienteCounters { get; set; }
    public DbSet<MedicalHistory> MedicalHistories { get; set; }
    public DbSet<Attachment> Attachments { get; set; }
    public DbSet<AdditionalPhone> AdditionalPhones { get; set; }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        // NOTE: The IEntityTypeConfiguration classes under Core/Configurations
        // (PatientsConfiguration, MedicalHistoriesConfiguration, BaseEntityConfiguration,
        // etc.) were never wired up via ApplyConfigurationsFromAssembly/ApplyConfiguration
        // before this change — the model has so far been built purely from conventions and
        // data annotations. Turning all of them on here would fold a large, unrelated schema
        // diff into the tenancy migration, so that clean-up is left for a separate pass.
        // TenantId is therefore configured directly below for every entity that derives
        // from BaseEntity, via reflection, independent of those (currently inactive) configs.
        foreach (var entityType in modelBuilder.Model.GetEntityTypes())
        {
            if (!typeof(BaseEntity).IsAssignableFrom(entityType.ClrType))
            {
                continue;
            }

            var entityBuilder = modelBuilder.Entity(entityType.ClrType);

            entityBuilder.Property(nameof(BaseEntity.TenantId)).IsRequired();

            // Every tenant-scoped query filters by TenantId, so an index is essential.
            entityBuilder.HasIndex(nameof(BaseEntity.TenantId));

            // Tenant isolation: no query against a tenant-scoped table can ever return
            // rows belonging to a different tenant than the one resolved for the current
            // request (see ICurrentTenantService / CurrentTenantService). If no tenant can
            // be resolved — e.g. a background job with no HTTP context — the filter
            // intentionally matches nothing rather than risk leaking data across tenants.
            entityBuilder.HasQueryFilter(BuildTenantFilter(entityType.ClrType));
        }
    }

    private LambdaExpression BuildTenantFilter(Type entityType)
    {
        var method = typeof(HistorialDbContext)
            .GetMethod(nameof(CreateTenantFilter), BindingFlags.NonPublic | BindingFlags.Instance)!
            .MakeGenericMethod(entityType);

        return (LambdaExpression)method.Invoke(this, null)!;
    }

    // ReSharper disable once UnusedMember.Local — invoked reflectively from BuildTenantFilter
    private LambdaExpression CreateTenantFilter<TEntity>() where TEntity : BaseEntity
    {
        Expression<Func<TEntity, bool>> filter = entity => entity.TenantId == _currentTenant.TenantId;
        return filter;
    }
}
