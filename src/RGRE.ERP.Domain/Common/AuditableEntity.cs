namespace RGRE.ERP.Domain.Common;

/// <summary>
/// Audit columns present on every legacy table (<c>CreatedBy</c>,
/// <c>ModificationDate</c> and friends). Stamped automatically by the
/// infrastructure audit interceptor on save; domain code never sets them.
/// </summary>
public abstract class AuditableEntity
{
    /// <summary>Legacy <c>CreatedBy</c> - id of the user that created the row.</summary>
    public Guid? CreatedBy { get; private set; }

    /// <summary>Legacy <c>CreationDate</c> - UTC timestamp of creation.</summary>
    public DateTime CreatedAt { get; private set; }

    /// <summary>Legacy <c>ModifiedBy</c> - id of the user that last modified the row.</summary>
    public Guid? ModifiedBy { get; private set; }

    /// <summary>Legacy <c>ModificationDate</c> - UTC timestamp of the last change.</summary>
    public DateTime? ModifiedAt { get; private set; }
}
