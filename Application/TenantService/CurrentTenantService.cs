using Core.Services;
using Microsoft.AspNetCore.Http;

namespace Application.TenantService;

/// <summary>
/// Resolves the current tenant from the "tenantId" claim embedded in the
/// authenticated user's JWT at login time (see UserService.GenerateJwtToken).
///
/// Registered as Scoped (one instance per HTTP request/DbContext scope) so the
/// resolved value stays stable for the lifetime of a request, and so EF Core's
/// global query filters in HistorialDbContext can capture it and use it to scope
/// every query to that tenant.
///
/// Anonymous endpoints (login, registration, health checks, etc.) have no
/// authenticated principal yet, so TenantId is simply null there — by design,
/// tenant-scoped query filters treat a null tenant as "match nothing".
/// </summary>
public class CurrentTenantService : ICurrentTenantService
{
    public Guid? TenantId { get; }

    public CurrentTenantService(IHttpContextAccessor httpContextAccessor)
    {
        var claimValue = httpContextAccessor.HttpContext?.User?
            .FindFirst("tenantId")?.Value;

        TenantId = Guid.TryParse(claimValue, out var tenantId) ? tenantId : null;
    }
}
