using RGRE.ERP.Domain.Common;

namespace RGRE.ERP.Domain.Inventory;

/// <summary>
/// A sellable/storable item. Replaces the legacy <c>Goods</c> table
/// (legacy 5-part property codes collapse into a simple description here).
/// </summary>
public sealed class Goods : AuditableEntity
{
    public Guid Id { get; private set; }

    /// <summary>Legacy <c>Code</c> (char(5) inside group) + group, kept as one business code.</summary>
    public string Code { get; private set; } = null!;

    public string Name { get; private set; } = null!;

    /// <summary>Legacy <c>Name2</c> - the second-language name.</summary>
    public string? Name2 { get; private set; }

    /// <summary>Legacy <c>Unit1ID</c> - base unit of measure.</summary>
    public Guid BaseUnitId { get; private set; }

    /// <summary>Legacy <c>BarCode</c>.</summary>
    public string? BarCode { get; private set; }

    /// <summary>Legacy <c>GoodType</c>: 0 = goods, 1 = service, 2 = raw material.</summary>
    public GoodsType GoodsType { get; private set; }

    /// <summary>Legacy <c>ConstPrice</c> - standard cost.</summary>
    public decimal? StandardCost { get; private set; }

    /// <summary>Legacy <c>SalePrice1</c> - default sale price.</summary>
    public decimal? SalePrice { get; private set; }

    /// <summary>Legacy <c>Enabled</c>.</summary>
    public bool Enabled { get; private set; }

    public string? Description { get; private set; }

    private Goods()
    {
    }

    public Goods(
        string code,
        string name,
        Guid baseUnitId,
        string? name2 = null,
        string? barCode = null,
        GoodsType goodsType = GoodsType.Goods,
        decimal? standardCost = null,
        decimal? salePrice = null,
        string? description = null)
    {
        if (string.IsNullOrWhiteSpace(code))
            throw new ArgumentException("Goods code is required.");

        if (string.IsNullOrWhiteSpace(name))
            throw new ArgumentException("Goods name is required.");

        if (baseUnitId == Guid.Empty)
            throw new ArgumentException("Base unit is required.");

        Id = Guid.NewGuid();
        Code = code.Trim();
        Name = name.Trim();
        Name2 = name2;
        BaseUnitId = baseUnitId;
        BarCode = barCode;
        GoodsType = goodsType;
        StandardCost = standardCost;
        SalePrice = salePrice;
        Enabled = true;
        Description = description;
    }

    public void Rename(string name, string? name2 = null)
    {
        if (string.IsNullOrWhiteSpace(name))
            throw new ArgumentException("Goods name is required.");

        Name = name.Trim();
        Name2 = name2;
    }

    public void SetPrices(decimal? standardCost, decimal? salePrice)
    {
        if (standardCost.HasValue && standardCost.Value < 0)
            throw new ArgumentException("Standard cost cannot be negative.");

        if (salePrice.HasValue && salePrice.Value < 0)
            throw new ArgumentException("Sale price cannot be negative.");

        StandardCost = standardCost;
        SalePrice = salePrice;
    }

    public void SetDescription(string? description) => Description = description;

    public void Disable()
    {
        Enabled = false;
    }

    public void Enable()
    {
        Enabled = true;
    }
}

public enum GoodsType
{
    Goods = 0,
    Service = 1,
    RawMaterial = 2,
}
