using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Core.Entities;

public class BaseEntity
{
    [Key]
    [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
    public Guid Id { get; set; }

    /// <summary>
    /// The tenant (clinic/practice) that owns this record. Every query against
    /// tenant-scoped entities is filtered by this value (see HistorialDbContext's
    /// global query filters), so it must always be set on creation.
    /// </summary>
    public Guid TenantId { get; set; }

    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }
}