using Microsoft.EntityFrameworkCore;
using RGRE.ERP.Application.Abstractions;
using RGRE.ERP.Domain.Accounting;
using RGRE.ERP.Domain.Inventory;
using RGRE.ERP.Domain.Inventory.Issues;
using RGRE.ERP.Domain.Inventory.Receipts;
using RGRE.ERP.Domain.Inventory.Transfers;
using RGRE.ERP.Domain.Partners;
using RGRE.ERP.Infrastructure.Persistence;

namespace RGRE.ERP.Infrastructure.Repositories;

public class GoodsRepository : IGoodsRepository
{
    private readonly ErpDbContext _db;

    public GoodsRepository(ErpDbContext db) => _db = db;

    public Task<Goods?> GetAsync(Guid id, CancellationToken cancellationToken = default)
        => _db.Goods.FirstOrDefaultAsync(x => x.Id == id, cancellationToken);

    public async Task<IReadOnlyList<Goods>> ListAsync(CancellationToken cancellationToken = default)
        => await _db.Goods.AsNoTracking().OrderBy(x => x.Code).ToListAsync(cancellationToken);

    public async Task AddAsync(Goods goods, CancellationToken cancellationToken = default)
        => await _db.Goods.AddAsync(goods, cancellationToken);
}

public class UnitRepository : IUnitRepository
{
    private readonly ErpDbContext _db;

    public UnitRepository(ErpDbContext db) => _db = db;

    public Task<Unit?> GetAsync(Guid id, CancellationToken cancellationToken = default)
        => _db.Units.FirstOrDefaultAsync(x => x.Id == id, cancellationToken);

    public async Task<IReadOnlyList<Unit>> ListAsync(CancellationToken cancellationToken = default)
        => await _db.Units.AsNoTracking().OrderBy(x => x.Name).ToListAsync(cancellationToken);

    public async Task AddAsync(Unit unit, CancellationToken cancellationToken = default)
        => await _db.Units.AddAsync(unit, cancellationToken);
}

public class WarehouseRepository : IWarehouseRepository
{
    private readonly ErpDbContext _db;

    public WarehouseRepository(ErpDbContext db) => _db = db;

    public Task<Warehouse?> GetAsync(Guid id, CancellationToken cancellationToken = default)
        => _db.Warehouses.FirstOrDefaultAsync(x => x.Id == id, cancellationToken);

    public async Task<IReadOnlyList<Warehouse>> ListAsync(CancellationToken cancellationToken = default)
        => await _db.Warehouses.AsNoTracking().OrderBy(x => x.Code).ToListAsync(cancellationToken);

    public async Task AddAsync(Warehouse warehouse, CancellationToken cancellationToken = default)
        => await _db.Warehouses.AddAsync(warehouse, cancellationToken);
}

public class StockMoveRepository : IStockMoveRepository
{
    private readonly ErpDbContext _db;

    public StockMoveRepository(ErpDbContext db) => _db = db;

    public async Task<IReadOnlyList<StockMove>> ListByDocumentAsync(
        Guid documentId, CancellationToken cancellationToken = default)
        => await _db.StockMoves.AsNoTracking()
            .Where(m => m.DocumentId == documentId)
            .OrderBy(m => m.MoveDate)
            .ToListAsync(cancellationToken);

    public async Task<IReadOnlyList<StockMove>> ListByGoodsAsync(
        Guid goodsId, CancellationToken cancellationToken = default)
        => await _db.StockMoves.AsNoTracking()
            .Where(m => m.GoodsId == goodsId)
            .OrderBy(m => m.MoveDate)
            .ToListAsync(cancellationToken);

    public async Task AddRangeAsync(IEnumerable<StockMove> moves, CancellationToken cancellationToken = default)
        => await _db.StockMoves.AddRangeAsync(moves, cancellationToken);
}

public class StockBalanceRepository : IStockBalanceRepository
{
    private readonly ErpDbContext _db;

    public StockBalanceRepository(ErpDbContext db) => _db = db;

    public Task<StockBalance?> GetAsync(Guid warehouseId, Guid goodsId, CancellationToken cancellationToken = default)
        => _db.StockBalances.FirstOrDefaultAsync(
            b => b.WarehouseId == warehouseId && b.GoodsId == goodsId,
            cancellationToken);

    public async Task<IReadOnlyList<StockBalance>> ListByWarehouseAsync(
        Guid warehouseId, CancellationToken cancellationToken = default)
        => await _db.StockBalances.AsNoTracking()
            .Where(b => b.WarehouseId == warehouseId)
            .ToListAsync(cancellationToken);

    public async Task AddAsync(StockBalance balance, CancellationToken cancellationToken = default)
    {
        var entry = _db.Entry(balance);

        if (entry.State == EntityState.Detached)
            await _db.StockBalances.AddAsync(balance, cancellationToken);
    }
}

public class ReceiptRepository : IReceiptRepository
{
    private readonly ErpDbContext _db;

    public ReceiptRepository(ErpDbContext db) => _db = db;

    public Task<Receipt?> GetAsync(Guid id, CancellationToken cancellationToken = default)
        => _db.Receipts.Include(r => r.Lines)
            .FirstOrDefaultAsync(x => x.Id == id, cancellationToken);

    public async Task AddAsync(Receipt receipt, CancellationToken cancellationToken = default)
        => await _db.Receipts.AddAsync(receipt, cancellationToken);

    public async Task<IReadOnlyList<Receipt>> ListAsync(CancellationToken cancellationToken = default)
        => await _db.Receipts.AsNoTracking().Include(r => r.Lines)
            .OrderByDescending(r => r.DocumentDate).ToListAsync(cancellationToken);
}

public class IssueRepository : IIssueRepository
{
    private readonly ErpDbContext _db;

    public IssueRepository(ErpDbContext db) => _db = db;

    public Task<Issue?> GetAsync(Guid id, CancellationToken cancellationToken = default)
        => _db.Issues.Include(i => i.Lines)
            .FirstOrDefaultAsync(x => x.Id == id, cancellationToken);

    public async Task AddAsync(Issue issue, CancellationToken cancellationToken = default)
        => await _db.Issues.AddAsync(issue, cancellationToken);

    public async Task<IReadOnlyList<Issue>> ListAsync(CancellationToken cancellationToken = default)
        => await _db.Issues.AsNoTracking().Include(i => i.Lines)
            .OrderByDescending(i => i.DocumentDate).ToListAsync(cancellationToken);
}

public class StockTransferRepository : IStockTransferRepository
{
    private readonly ErpDbContext _db;

    public StockTransferRepository(ErpDbContext db) => _db = db;

    public Task<StockTransfer?> GetAsync(Guid id, CancellationToken cancellationToken = default)
        => _db.StockTransfers.Include(t => t.Lines)
            .FirstOrDefaultAsync(x => x.Id == id, cancellationToken);

    public async Task AddAsync(StockTransfer transfer, CancellationToken cancellationToken = default)
        => await _db.StockTransfers.AddAsync(transfer, cancellationToken);

    public async Task<IReadOnlyList<StockTransfer>> ListAsync(CancellationToken cancellationToken = default)
        => await _db.StockTransfers.AsNoTracking().Include(t => t.Lines)
            .OrderByDescending(t => t.DocumentDate).ToListAsync(cancellationToken);
}

public class PartnerRepository : IPartnerRepository
{
    private readonly ErpDbContext _db;

    public PartnerRepository(ErpDbContext db) => _db = db;

    public Task<Partner?> GetAsync(Guid id, CancellationToken cancellationToken = default)
        => _db.Partners.FirstOrDefaultAsync(x => x.Id == id, cancellationToken);

    public async Task<IReadOnlyList<Partner>> ListAsync(CancellationToken cancellationToken = default)
        => await _db.Partners.AsNoTracking().OrderBy(x => x.Code).ToListAsync(cancellationToken);

    public async Task AddAsync(Partner partner, CancellationToken cancellationToken = default)
        => await _db.Partners.AddAsync(partner, cancellationToken);
}

public class LedgerAccountRepository : ILedgerAccountRepository
{
    private readonly ErpDbContext _db;

    public LedgerAccountRepository(ErpDbContext db) => _db = db;

    public Task<LedgerAccount?> GetAsync(Guid id, CancellationToken cancellationToken = default)
        => _db.LedgerAccounts.FirstOrDefaultAsync(x => x.Id == id, cancellationToken);

    public Task<LedgerAccount?> GetByCodeAsync(string code, CancellationToken cancellationToken = default)
        => _db.LedgerAccounts.FirstOrDefaultAsync(x => x.Code == code, cancellationToken);

    public async Task<IReadOnlyList<LedgerAccount>> ListAsync(CancellationToken cancellationToken = default)
        => await _db.LedgerAccounts.AsNoTracking().OrderBy(x => x.Code).ToListAsync(cancellationToken);

    public async Task AddAsync(LedgerAccount account, CancellationToken cancellationToken = default)
        => await _db.LedgerAccounts.AddAsync(account, cancellationToken);
}

public class VoucherRepository : IVoucherRepository
{
    private readonly ErpDbContext _db;

    public VoucherRepository(ErpDbContext db) => _db = db;

    public Task<Voucher?> GetAsync(Guid id, CancellationToken cancellationToken = default)
        => _db.Vouchers.Include(v => v.Lines)
            .FirstOrDefaultAsync(x => x.Id == id, cancellationToken);

    public async Task<IReadOnlyList<Voucher>> ListAsync(CancellationToken cancellationToken = default)
        => await _db.Vouchers.AsNoTracking().Include(v => v.Lines)
            .OrderByDescending(v => v.Date).ToListAsync(cancellationToken);

    public async Task AddAsync(Voucher voucher, CancellationToken cancellationToken = default)
        => await _db.Vouchers.AddAsync(voucher, cancellationToken);
}

public class UnitOfWork : IUnitOfWork
{
    private readonly ErpDbContext _db;

    public UnitOfWork(ErpDbContext db) => _db = db;

    public Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
        => _db.SaveChangesAsync(cancellationToken);
}
