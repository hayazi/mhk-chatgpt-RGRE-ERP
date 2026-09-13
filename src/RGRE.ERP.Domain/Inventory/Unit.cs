using RGRE.ERP.Domain.Common;

namespace RGRE.ERP.Domain.Inventory;

/// <summary>
/// Unit of measure. Replaces the legacy <c>Unit</c> table.
/// </summary>
public sealed class Unit : AuditableEntity
{
    public Guid Id { get; private set; }

    public string Name { get; private set; } = null!;

    /// <summary>Legacy <c>UnitRatio</c> style factor, 1 for base units.</summary>
    public decimal ConversionFactor { get; private set; }

    /// <summary>Reference to the base unit this unit converts to, if not itself a base unit.</summary>
    public Guid? BaseUnitId { get; private set; }

    private Unit()
    {
    }

    public Unit(string name, decimal conversionFactor = 1m, Guid? baseUnitId = null)
    {
        if (string.IsNullOrWhiteSpace(name))
            throw new ArgumentException("Unit name is required.");

        if (conversionFactor <= 0)
            throw new ArgumentException("Conversion factor must be greater than zero.");

        if (baseUnitId.HasValue && baseUnitId.Value == Guid.Empty)
            baseUnitId = null;

        Id = Guid.NewGuid();
        Name = name.Trim();
        ConversionFactor = conversionFactor;
        BaseUnitId = baseUnitId;
    }
}
