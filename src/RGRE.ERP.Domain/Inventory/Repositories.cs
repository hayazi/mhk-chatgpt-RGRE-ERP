using RGRE.ERP.Domain.Inventory.Issues;
using RGRE.ERP.Domain.Inventory.Receipts;
using RGRE.ERP.Domain.Inventory.Transfers;

namespace RGRE.ERP.Domain.Inventory;

public interface IGoodsRepository
{
    Task<Goods?> GetAsync(Guid id, CancellationToken cancellationToken = default);

    Task<IReadOnlyList<Goods>> ListAsync(CancellationToken cancellationToken = default);

    Task AddAsync(Goods goods, CancellationToken cancellationToken = default);
}

public interface IUnitRepository
{
    Task<Unit?> GetAsync(Guid id, CancellationToken cancellationToken = default);

    Task<IReadOnlyList<Unit>> ListAsync(CancellationToken cancellationToken = default);

    Task AddAsync(Unit unit, CancellationToken cancellationToken = default);
}

public interface IWarehouseRepository
{
    Task<Warehouse?> GetAsync(Guid id, CancellationToken cancellationToken = default);

    Task<IReadOnlyList<Warehouse>> ListAsync(CancellationToken cancellationToken = default);

    Task AddAsync(Warehouse warehouse, CancellationToken cancellationToken = default);
}

public interface IStockMoveRepository
{
    Task<IReadOnlyList<StockMove>> ListByDocumentAsync(
        Guid documentId,
        CancellationToken cancellationToken = default);

    Task AddRangeAsync(IEnumerable<StockMove> moves, CancellationToken cancellationToken = default);

    /// <summary>All moves for a goods across warehouses, ordered by date.</summary>
    Task<IReadOnlyList<StockMove>> ListByGoodsAsync(
        Guid goodsId,
        CancellationToken cancellationToken = default);
}

public interface IStockBalanceRepository
{
    Task<StockBalance?> GetAsync(
        Guid warehouseId,
        Guid goodsId,
        CancellationToken cancellationToken = default);

    Task<IReadOnlyList<StockBalance>> ListByWarehouseAsync(
        Guid warehouseId,
        CancellationToken cancellationToken = default);

    Task AddAsync(StockBalance balance, CancellationToken cancellationToken = default);
}

public interface IIssueRepository
{
    Task<Issue?> GetAsync(Guid id, CancellationToken cancellationToken = default);

    Task<IReadOnlyList<Issue>> ListAsync(CancellationToken cancellationToken = default);

    Task AddAsync(Issue issue, CancellationToken cancellationToken = default);
}

public interface IStockTransferRepository
{
    Task<StockTransfer?> GetAsync(Guid id, CancellationToken cancellationToken = default);

    Task<IReadOnlyList<StockTransfer>> ListAsync(CancellationToken cancellationToken = default);

    Task AddAsync(StockTransfer transfer, CancellationToken cancellationToken = default);
}
