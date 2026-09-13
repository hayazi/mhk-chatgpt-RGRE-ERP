using RGRE.ERP.Application.Abstractions;
using RGRE.ERP.Domain.Inventory;

namespace RGRE.ERP.Application.Inventory;

// ---------- Goods ----------

public sealed class CreateGoodsDto
{
    public string Code { get; set; } = null!;
    public string Name { get; set; } = null!;
    public string? Name2 { get; set; }
    public Guid BaseUnitId { get; set; }
    public string? BarCode { get; set; }
    public string GoodsType { get; set; } = "Goods";
    public decimal? StandardCost { get; set; }
    public decimal? SalePrice { get; set; }
    public string? Description { get; set; }
}

public sealed class GoodsDto
{
    public Guid Id { get; set; }
    public string Code { get; set; } = null!;
    public string Name { get; set; } = null!;
    public string? Name2 { get; set; }
    public Guid BaseUnitId { get; set; }
    public string? BarCode { get; set; }
    public string GoodsType { get; set; } = null!;
    public decimal? StandardCost { get; set; }
    public decimal? SalePrice { get; set; }
    public bool Enabled { get; set; }
    public string? Description { get; set; }
}

public interface IGoodsAppService
{
    Task<Guid> CreateAsync(CreateGoodsDto input, CancellationToken cancellationToken = default);

    Task<IReadOnlyList<GoodsDto>> ListAsync(CancellationToken cancellationToken = default);

    Task DisableAsync(Guid id, CancellationToken cancellationToken = default);

    Task EnableAsync(Guid id, CancellationToken cancellationToken = default);
}

public sealed class GoodsAppService : IGoodsAppService
{
    private readonly IGoodsRepository _goods;
    private readonly IUnitOfWork _unitOfWork;

    public GoodsAppService(IGoodsRepository goods, IUnitOfWork unitOfWork)
    {
        _goods = goods;
        _unitOfWork = unitOfWork;
    }

    public async Task<Guid> CreateAsync(CreateGoodsDto input, CancellationToken cancellationToken = default)
    {
        var goodsType = input.GoodsType.Equals("Service", StringComparison.OrdinalIgnoreCase)
            ? GoodsType.Service
            : input.GoodsType.Equals("RawMaterial", StringComparison.OrdinalIgnoreCase)
                ? GoodsType.RawMaterial
                : GoodsType.Goods;

        var goods = new Goods(
            input.Code,
            input.Name,
            input.BaseUnitId,
            input.Name2,
            input.BarCode,
            goodsType,
            input.StandardCost,
            input.SalePrice,
            input.Description);

        await _goods.AddAsync(goods, cancellationToken);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        return goods.Id;
    }

    public async Task<IReadOnlyList<GoodsDto>> ListAsync(CancellationToken cancellationToken = default)
    {
        var list = await _goods.ListAsync(cancellationToken);

        return list.Select(g => new GoodsDto
        {
            Id = g.Id,
            Code = g.Code,
            Name = g.Name,
            Name2 = g.Name2,
            BaseUnitId = g.BaseUnitId,
            BarCode = g.BarCode,
            GoodsType = g.GoodsType.ToString(),
            StandardCost = g.StandardCost,
            SalePrice = g.SalePrice,
            Enabled = g.Enabled,
            Description = g.Description,
        }).ToList();
    }

    public async Task DisableAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var goods = await _goods.GetAsync(id, cancellationToken)
            ?? throw new KeyNotFoundException($"Goods '{id}' was not found.");

        goods.Disable();
        await _unitOfWork.SaveChangesAsync(cancellationToken);
    }

    public async Task EnableAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var goods = await _goods.GetAsync(id, cancellationToken)
            ?? throw new KeyNotFoundException($"Goods '{id}' was not found.");

        goods.Enable();
        await _unitOfWork.SaveChangesAsync(cancellationToken);
    }
}

// ---------- Units ----------

public sealed class CreateUnitDto
{
    public string Name { get; set; } = null!;
    public decimal ConversionFactor { get; set; } = 1m;
    public Guid? BaseUnitId { get; set; }
}

public interface IUnitAppService
{
    Task<Guid> CreateAsync(CreateUnitDto input, CancellationToken cancellationToken = default);

    Task<IReadOnlyList<Unit>> ListAsync(CancellationToken cancellationToken = default);
}

public sealed class UnitAppService : IUnitAppService
{
    private readonly IUnitRepository _units;
    private readonly IUnitOfWork _unitOfWork;

    public UnitAppService(IUnitRepository units, IUnitOfWork unitOfWork)
    {
        _units = units;
        _unitOfWork = unitOfWork;
    }

    public async Task<Guid> CreateAsync(CreateUnitDto input, CancellationToken cancellationToken = default)
    {
        var unit = new Unit(input.Name, input.ConversionFactor, input.BaseUnitId);

        await _units.AddAsync(unit, cancellationToken);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        return unit.Id;
    }

    public async Task<IReadOnlyList<Unit>> ListAsync(CancellationToken cancellationToken = default)
        => await _units.ListAsync(cancellationToken);
}

// ---------- Warehouses ----------

public sealed class CreateWarehouseDto
{
    public string Code { get; set; } = null!;
    public string Name { get; set; } = null!;
    public Guid? ParentId { get; set; }
    public string? Address { get; set; }
    public string? Telephone { get; set; }
    public string? Description { get; set; }
}

public interface IWarehouseAppService
{
    Task<Guid> CreateAsync(CreateWarehouseDto input, CancellationToken cancellationToken = default);

    Task<IReadOnlyList<Warehouse>> ListAsync(CancellationToken cancellationToken = default);
}

public sealed class WarehouseAppService : IWarehouseAppService
{
    private readonly IWarehouseRepository _warehouses;
    private readonly IUnitOfWork _unitOfWork;

    public WarehouseAppService(IWarehouseRepository warehouses, IUnitOfWork unitOfWork)
    {
        _warehouses = warehouses;
        _unitOfWork = unitOfWork;
    }

    public async Task<Guid> CreateAsync(CreateWarehouseDto input, CancellationToken cancellationToken = default)
    {
        var warehouse = new Warehouse(
            input.Code,
            input.Name,
            input.ParentId,
            input.Address,
            input.Telephone,
            input.Description);

        await _warehouses.AddAsync(warehouse, cancellationToken);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        return warehouse.Id;
    }

    public async Task<IReadOnlyList<Warehouse>> ListAsync(CancellationToken cancellationToken = default)
        => await _warehouses.ListAsync(cancellationToken);
}

// ---------- Stock queries ----------

public sealed class StockBalanceDto
{
    public Guid WarehouseId { get; set; }
    public Guid GoodsId { get; set; }
    public decimal Quantity { get; set; }
    public decimal AverageCost { get; set; }
    public decimal TotalValue { get; set; }
}

public interface IStockQueryService
{
    Task<IReadOnlyList<StockBalanceDto>> GetByWarehouseAsync(
        Guid warehouseId,
        CancellationToken cancellationToken = default);

    Task<IReadOnlyList<StockMove>> GetMovesAsync(
        Guid goodsId,
        CancellationToken cancellationToken = default);
}

public sealed class StockQueryService : IStockQueryService
{
    private readonly IStockBalanceRepository _balances;
    private readonly IStockMoveRepository _moves;

    public StockQueryService(IStockBalanceRepository balances, IStockMoveRepository moves)
    {
        _balances = balances;
        _moves = moves;
    }

    public async Task<IReadOnlyList<StockBalanceDto>> GetByWarehouseAsync(
        Guid warehouseId,
        CancellationToken cancellationToken = default)
    {
        var balances = await _balances.ListByWarehouseAsync(warehouseId, cancellationToken);

        return balances.Select(b => new StockBalanceDto
        {
            WarehouseId = b.WarehouseId,
            GoodsId = b.GoodsId,
            Quantity = b.Quantity,
            AverageCost = b.GetAverageCost(),
            TotalValue = b.TotalValueIn - b.TotalValueOut,
        }).ToList();
    }

    public async Task<IReadOnlyList<StockMove>> GetMovesAsync(
        Guid goodsId,
        CancellationToken cancellationToken = default)
        => await _moves.ListByGoodsAsync(goodsId, cancellationToken);
}
