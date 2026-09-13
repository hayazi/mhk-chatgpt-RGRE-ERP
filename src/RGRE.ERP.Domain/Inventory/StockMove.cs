using RGRE.ERP.Domain.Inventory.Receipts;

using RGRE.ERP.Domain.Common;

namespace RGRE.ERP.Domain.Inventory;

/// <summary>
/// Movement direction, from the legacy <c>Resid</c> / <c>Khorooj</c> split.
/// </summary>
public enum StockMoveDirection
{
    In = 1,
    Out = 2,
}

/// <summary>
/// One immutable stock movement line created when a document is posted.
/// Replaces the legacy <c>ResidDetail</c> / <c>KhoroojDetail</c> rows.
/// </summary>
public sealed class StockMove : AuditableEntity
{
    public Guid Id { get; private set; }

    public Guid DocumentId { get; private set; }

    /// <summary>Legacy <c>ResidID</c> / <c>KhoroojID</c>.</summary>
    public StockMoveDirection Direction { get; private set; }

    public DateTime MoveDate { get; private set; }

    public Guid WarehouseId { get; private set; }

    public Guid GoodsId { get; private set; }

    public Guid UnitId { get; private set; }

    /// <summary>Quantity expressed in the document's unit.</summary>
    public decimal Quantity { get; private set; }

    /// <summary>Quantity expressed in the goods' base unit.</summary>
    public decimal BaseQuantity { get; private set; }

    /// <summary>Unit price used for valuation.</summary>
    public decimal UnitPrice { get; private set; }

    public string? Description { get; private set; }

    private StockMove()
    {
    }

    public StockMove(
        Guid documentId,
        StockMoveDirection direction,
        DateTime moveDate,
        Guid warehouseId,
        Guid goodsId,
        Guid unitId,
        decimal quantity,
        decimal unitPrice,
        string? description)
    {
        Id = Guid.NewGuid();
        DocumentId = documentId;
        Direction = direction;
        MoveDate = moveDate;
        WarehouseId = warehouseId;
        GoodsId = goodsId;
        UnitId = unitId;
        Quantity = quantity;
        BaseQuantity = quantity;
        UnitPrice = unitPrice;
        Description = description;
    }

    public void SetBaseQuantity(decimal baseQuantity)
    {
        if (baseQuantity <= 0)
            throw new ArgumentException("Base quantity must be greater than zero.");

        BaseQuantity = baseQuantity;
    }
}
