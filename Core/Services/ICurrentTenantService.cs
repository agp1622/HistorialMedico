namespace Core.Services;

/// <summary>
/// Resolves the tenant (clinic/practice) associated with the current request.
///
/// Implementations typically read a "tenantId" claim embedded in the authenticated
/// user's JWT at login time (see UserService.GenerateJwtToken and
/// Application.TenantService.CurrentTenantService).
///
/// A null TenantId means no tenant could be resolved — e.g. anonymous requests
/// (login, registration), background jobs with no HTTP context, or platform-level
/// super-admins. Tenant-scoped query filters (see HistorialDbContext) are written to
/// return no rows in that case rather than risk leaking data across tenants.
/// </summary>
public interface ICurrentTenantService
{
    Guid? TenantId { get; }
}
