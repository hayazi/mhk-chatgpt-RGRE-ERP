using System.Text;
using RGRE.ERP.Application.Abstractions;
using RGRE.ERP.Domain.Accounting;
using RGRE.ERP.Domain.Inventory;
using RGRE.ERP.Domain.Inventory.Issues;
using RGRE.ERP.Domain.Inventory.Receipts;
using RGRE.ERP.Domain.Inventory.Transfers;

namespace RGRE.ERP.Application.Tests;

public sealed class InMemoryUnitOfWork : IUnitOfWork
{
    public int SaveCount { get; private set; }

    public Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
    {
        SaveCount++;
        return Task.FromResult(SaveCount);
    }
}

public sealed class InMemoryGoodsRepository : IGoodsRepository
{
    public readonly Dictionary<Guid, Goods> Store = new();

    public Task<Goods?> GetAsync(Guid id, CancellationToken cancellationToken = default)
        => Task.FromResult(Store.TryGetValue(id, out var g) ? g : null);

    public Task<IReadOnlyList<Goods>> ListAsync(CancellationToken cancellationToken = default)
        => Task.FromResult((IReadOnlyList<Goods>)Store.Values.ToList());

    public Task AddAsync(Goods goods, CancellationToken cancellationToken = default)
    {
        Store[goods.Id] = goods;
        return Task.CompletedTask;
    }
}

public sealed class InMemoryUnitRepository : IUnitRepository
{
    public readonly Dictionary<Guid, Unit> Store = new();

    public Task<Unit?> GetAsync(Guid id, CancellationToken cancellationToken = default)
        => Task.FromResult(Store.TryGetValue(id, out var u) ? u : null);

    public Task<IReadOnlyList<Unit>> ListAsync(CancellationToken cancellationToken = default)
        => Task.FromResult((IReadOnlyList<Unit>)Store.Values.ToList());

    public Task AddAsync(Unit unit, CancellationToken cancellationToken = default)
    {
        Store[unit.Id] = unit;
        return Task.CompletedTask;
    }
}

public sealed class InMemoryReceiptRepository : IReceiptRepository
{
    public readonly Dictionary<Guid, Receipt> Store = new();

    public Task<Receipt?> GetAsync(Guid id, CancellationToken cancellationToken = default)
        => Task.FromResult(Store.TryGetValue(id, out var r) ? r : null);

    public Task<IReadOnlyList<Receipt>> ListAsync(CancellationToken cancellationToken = default)
        => Task.FromResult((IReadOnlyList<Receipt>)Store.Values.ToList());

    public Task AddAsync(Receipt receipt, CancellationToken cancellationToken = default)
    {
        Store[receipt.Id] = receipt;
        return Task.CompletedTask;
    }
}

public sealed class InMemoryIssueRepository : IIssueRepository
{
    public readonly Dictionary<Guid, Issue> Store = new();

    public Task<Issue?> GetAsync(Guid id, CancellationToken cancellationToken = default)
        => Task.FromResult(Store.TryGetValue(id, out var i) ? i : null);

    public Task<IReadOnlyList<Issue>> ListAsync(CancellationToken cancellationToken = default)
        => Task.FromResult((IReadOnlyList<Issue>)Store.Values.ToList());

    public Task AddAsync(Issue issue, CancellationToken cancellationToken = default)
    {
        Store[issue.Id] = issue;
        return Task.CompletedTask;
    }
}

public sealed class InMemoryTransferRepository : IStockTransferRepository
{
    public readonly Dictionary<Guid, StockTransfer> Store = new();

    public Task<StockTransfer?> GetAsync(Guid id, CancellationToken cancellationToken = default)
        => Task.FromResult(Store.TryGetValue(id, out var t) ? t : null);

    public Task<IReadOnlyList<StockTransfer>> ListAsync(CancellationToken cancellationToken = default)
        => Task.FromResult((IReadOnlyList<StockTransfer>)Store.Values.ToList());

    public Task AddAsync(StockTransfer transfer, CancellationToken cancellationToken = default)
    {
        Store[transfer.Id] = transfer;
        return Task.CompletedTask;
    }
}

public sealed class InMemoryStockMoveRepository : IStockMoveRepository
{
    public readonly List<StockMove> Moves = new();

    public Task<IReadOnlyList<StockMove>> ListByDocumentAsync(
        Guid documentId, CancellationToken cancellationToken = default)
        => Task.FromResult((IReadOnlyList<StockMove>)Moves.Where(m => m.DocumentId == documentId).ToList());

    public Task<IReadOnlyList<StockMove>> ListByGoodsAsync(
        Guid goodsId, CancellationToken cancellationToken = default)
        => Task.FromResult((IReadOnlyList<StockMove>)Moves.Where(m => m.GoodsId == goodsId).ToList());

    public Task AddRangeAsync(IEnumerable<StockMove> moves, CancellationToken cancellationToken = default)
    {
        Moves.AddRange(moves);
        return Task.CompletedTask;
    }
}

public sealed class StaticUserContext : ICurrentUserContext
{
    public static readonly StaticUserContext Anonymous = new();

    public bool IsAuthenticated { get; init; }

    public Guid? UserId { get; init; }

    public string? UserName { get; init; }

    public string? DisplayName { get; init; }

    public Guid? CompanyId { get; init; }

    public Guid? BranchId { get; init; }

    public Guid? FiscalYearId { get; init; }

    public bool HasPermission(string permission) => true;
}

public sealed class FakePasswordHasher : IPasswordHasher
{
    public string Hash(string password)
        => $"fake.{Convert.ToBase64String(Encoding.UTF8.GetBytes(password))}";

    public bool Verify(string password, string storedHash)
        => storedHash == Hash(password);
}

public sealed class InMemoryFiscalYearRepository : IFiscalYearRepository
{
    public readonly List<FiscalYear> Store = new();

    public Task<FiscalYear?> GetAsync(Guid id, CancellationToken cancellationToken = default)
        => Task.FromResult(Store.FirstOrDefault(f => f.Id == id));

    public Task<FiscalYear?> GetOpenAsync(CancellationToken cancellationToken = default)
        => Task.FromResult(Store.FirstOrDefault(f => !f.IsClosed));

    public Task<IReadOnlyList<FiscalYear>> ListAsync(CancellationToken cancellationToken = default)
        => Task.FromResult((IReadOnlyList<FiscalYear>)Store.ToList());

    public Task AddAsync(FiscalYear fiscalYear, CancellationToken cancellationToken = default)
    {
        Store.Add(fiscalYear);
        return Task.CompletedTask;
    }
}

public sealed class InMemoryStockBalanceRepository : IStockBalanceRepository
{
    public readonly Dictionary<(Guid WarehouseId, Guid GoodsId), StockBalance> Store = new();

    public Task<StockBalance?> GetAsync(Guid warehouseId, Guid goodsId, CancellationToken cancellationToken = default)
        => Task.FromResult(Store.TryGetValue((warehouseId, goodsId), out var b) ? b : null);

    public Task<IReadOnlyList<StockBalance>> ListByWarehouseAsync(
        Guid warehouseId, CancellationToken cancellationToken = default)
        => Task.FromResult((IReadOnlyList<StockBalance>)Store.Values
            .Where(b => b.WarehouseId == warehouseId).ToList());

    public Task AddAsync(StockBalance balance, CancellationToken cancellationToken = default)
    {
        Store[(balance.WarehouseId, balance.GoodsId)] = balance;
        return Task.CompletedTask;
    }
}
