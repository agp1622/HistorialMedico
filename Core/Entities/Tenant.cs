using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Core.Entities;

/// <summary>
/// Represents a single customer organization (a clinic / practice) in the multi-tenant
/// system. Every tenant-scoped record (patients, users, attachments, etc.) is associated
/// with exactly one Tenant, and no tenant can ever see another tenant's data.
/// </summary>
public class Tenant
{
    [Key]
    [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
    public Guid Id { get; set; }

    /// <summary>
    /// Display name of the clinic/practice, e.g. "Cirugía Sureda".
    /// </summary>
    [Required]
    [MaxLength(200)]
    public string Name { get; set; } = string.Empty;

    /// <summary>
    /// URL-friendly unique identifier used for subdomain routing, e.g. "cirugia-sureda"
    /// for cirugia-sureda.historialmedico.app. Also usable as a lookup key during
    /// tenant resolution.
    /// </summary>
    [Required]
    [MaxLength(100)]
    public string Slug { get; set; } = string.Empty;

    /// <summary>
    /// Subscription/billing status: "trialing", "active", "past_due", "canceled".
    /// Kept as a simple string for now; can be normalized into its own table once
    /// billing is implemented (see Plan/Subscription work in a later phase).
    /// </summary>
    [MaxLength(50)]
    public string BillingStatus { get; set; } = "trialing";

    /// <summary>
    /// Plan identifier the tenant is currently on, e.g. "solo", "clinic", "growth".
    /// </summary>
    [MaxLength(50)]
    public string Plan { get; set; } = "solo";

    /// <summary>
    /// Whether the tenant is active. A soft "kill switch" for suspending access
    /// (e.g. on non-payment) without deleting any data.
    /// </summary>
    public bool IsActive { get; set; } = true;

    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }
}
