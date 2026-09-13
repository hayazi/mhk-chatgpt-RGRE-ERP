using RGRE.ERP.Domain.Common;

namespace RGRE.ERP.Domain.Inventory;

/// <summary>
/// Read-model of on-hand stock per warehouse and goods.
/// Replaces the legacy <c>Analyze</c> / <c>HazineVaKosoratAnalyze</c>
/// quantity tables.
/// </summary>
public sealed class StockBalance : AuditableEntity
{
    public Guid WarehouseId { get; private set; }

    public Guid GoodsId { get; private set; }

    public decimal Quantity { get; private set; }

    /// <summary>Total inbound value (moving-average input).</summary>
    public decimal TotalValueIn { get; private set; }

    /// <summary>Total outbound value (moving-average output).</summary>
    public decimal TotalValueOut { get; private set; }

    /// <summary>Cumulative inbound base quantity, used to weight the average cost.</summary>
    public decimal TotalInQuantity { get; private set; }

    private StockBalance()
    {
    }

    public StockBalance(Guid warehouseId, Guid goodsId)
    {
        WarehouseId = warehouseId;
        GoodsId = goodsId;
    }

    public void Apply(decimal baseQuantityDelta, decimal valueDelta)
    {
        Quantity += baseQuantityDelta;
        if (baseQuantityDelta > 0)
        {
            TotalValueIn += valueDelta;
            TotalInQuantity += baseQuantityDelta;
        }
        else
        {
            TotalValueOut += Math.Abs(valueDelta);
        }
    }

    /// <summary>Moving-average unit cost: total inbound value / total inbound quantity.</summary>
    public decimal GetAverageCost()
    {
        return TotalInQuantity != 0
            ? TotalValueIn / TotalInQuantity
            : 0m;
    }
}
