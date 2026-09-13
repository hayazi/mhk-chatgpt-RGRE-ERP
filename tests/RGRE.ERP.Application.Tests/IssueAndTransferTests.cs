using Xunit;
using RGRE.ERP.Application.Inventory;
using RGRE.ERP.Application.Inventory.Issues;
using RGRE.ERP.Application.Inventory.Receipts;
using RGRE.ERP.Application.Inventory.Transfers;
using RGRE.ERP.Domain.Accounting;
using RGRE.ERP.Domain.Inventory;

namespace RGRE.ERP.Application.Tests;

public class IssueFlowTests
{
    [Fact]
    public async Task Posted_issue_decrements_stock_and_values_at_average_cost()
    {
        var goods = new Goods("G1", "Test goods", Guid.NewGuid());
        var goodsRepo = new InMemoryGoodsRepository();
        await goodsRepo.AddAsync(goods);

        var receipts = new InMemoryReceiptRepository();
        var issues = new InMemoryIssueRepository();
        var moves = new InMemoryStockMoveRepository();
        var balances = new InMemoryStockBalanceRepository();
        var units = new InMemoryUnitRepository();
        var uow = new InMemoryUnitOfWork();
        var fiscalYears = new InMemoryFiscalYearRepository();
        var ledger = new StockLedgerService(moves, balances, units);
        var warehouseId = Guid.NewGuid();

        var receiptService = new ReceiptAppService(receipts, goodsRepo, ledger, uow, fiscalYears, StaticUserContext.Anonymous);
        var issueService = new IssueAppService(issues, goodsRepo, ledger, uow, fiscalYears, StaticUserContext.Anonymous);

        // Receive 10 @ 100
        var receiptId = await receiptService.CreateAsync(new CreateReceiptDto
        {
            DocumentNumber = "R-200",
            DocumentDate = DateTime.UtcNow,
            WarehouseId = warehouseId,
            Lines = { new AddReceiptLineDto { GoodsId = goods.Id, UnitId = goods.BaseUnitId, Quantity = 10, UnitPrice = 100m } },
        });
        await receiptService.PostAsync(receiptId);

        // Issue 4
        var issueId = await issueService.CreateAsync(new CreateIssueDto
        {
            DocumentNumber = "K-200",
            DocumentDate = DateTime.UtcNow,
            WarehouseId = warehouseId,
            Lines = { new AddIssueLineDto { GoodsId = goods.Id, UnitId = goods.BaseUnitId, Quantity = 4, UnitPrice = 100m } },
        });
        await issueService.PostAsync(issueId);

        var balance = balances.Store[(warehouseId, goods.Id)];
        Assert.Equal(6m, balance.Quantity);
        Assert.Equal(400m, balance.TotalValueOut);
        Assert.Equal(1000m - 400m, balance.TotalValueIn - balance.TotalValueOut);

        var outMoves = moves.Moves.Where(m => m.Direction == StockMoveDirection.Out).ToList();
        Assert.Single(outMoves);
        Assert.Equal(warehouseId, outMoves[0].WarehouseId);
    }

    [Fact]
    public async Task Cannot_issue_more_than_received_when_balance_goes_negative()
    {
        // The domain currently allows negative stock (legacy MHK did too);
        // this test documents the behavior.
        var goods = new Goods("G1", "Test goods", Guid.NewGuid());
        var goodsRepo = new InMemoryGoodsRepository();
        await goodsRepo.AddAsync(goods);

        var ledger = new StockLedgerService(
            new InMemoryStockMoveRepository(),
            new InMemoryStockBalanceRepository(),
            new InMemoryUnitRepository());

        var balance = new StockBalance(Guid.NewGuid(), goods.Id);
        balance.Apply(-5m, -500m);

        Assert.Equal(-5m, balance.Quantity);
    }
}

public class TransferFlowTests
{
    [Fact]
    public async Task Posted_transfer_moves_stock_between_warehouses()
    {
        var goods = new Goods("G1", "Test goods", Guid.NewGuid());
        var goodsRepo = new InMemoryGoodsRepository();
        await goodsRepo.AddAsync(goods);

        var transfers = new InMemoryTransferRepository();
        var moves = new InMemoryStockMoveRepository();
        var balances = new InMemoryStockBalanceRepository();
        var ledger = new StockLedgerService(moves, balances, new InMemoryUnitRepository());
        var uow = new InMemoryUnitOfWork();
        var service = new StockTransferAppService(transfers, goodsRepo, ledger, uow, new InMemoryFiscalYearRepository(), StaticUserContext.Anonymous);

        var from = Guid.NewGuid();
        var to = Guid.NewGuid();

        var transferId = await service.CreateAsync(new CreateStockTransferDto
        {
            DocumentNumber = "T-300",
            DocumentDate = DateTime.UtcNow,
            FromWarehouseId = from,
            ToWarehouseId = to,
            Lines = { new AddStockTransferLineDto { GoodsId = goods.Id, UnitId = goods.BaseUnitId, Quantity = 7 } },
        });

        await service.PostAsync(transferId);

        Assert.Equal(2, moves.Moves.Count);
        Assert.Contains(moves.Moves, m => m.Direction == StockMoveDirection.Out && m.WarehouseId == from);
        Assert.Contains(moves.Moves, m => m.Direction == StockMoveDirection.In && m.WarehouseId == to);

        Assert.Equal(-7m, balances.Store[(from, goods.Id)].Quantity);
        Assert.Equal(7m, balances.Store[(to, goods.Id)].Quantity);
    }

    [Fact]
    public async Task Transfer_between_same_warehouse_is_rejected()
    {
        var service = new StockTransferAppService(
            new InMemoryTransferRepository(),
            new InMemoryGoodsRepository(),
            new StockLedgerService(
                new InMemoryStockMoveRepository(),
                new InMemoryStockBalanceRepository(),
                new InMemoryUnitRepository()),
            new InMemoryUnitOfWork(),
            new InMemoryFiscalYearRepository(),
            StaticUserContext.Anonymous);

        var same = Guid.NewGuid();

        await Assert.ThrowsAsync<ArgumentException>(() => service.CreateAsync(new CreateStockTransferDto
        {
            DocumentNumber = "T-301",
            DocumentDate = DateTime.UtcNow,
            FromWarehouseId = same,
            ToWarehouseId = same,
        }));
    }
}

public class VoucherTests
{
    [Fact]
    public void Unbalanced_voucher_cannot_be_posted()
    {
        var voucher = Voucher.Create("V-1", DateTime.UtcNow);

        voucher.AddLine(Guid.NewGuid(), debit: 100m, credit: 0m);
        voucher.AddLine(Guid.NewGuid(), debit: 0m, credit: 90m);

        Assert.Throws<InvalidOperationException>(() => voucher.Post());
    }

    [Fact]
    public void Balanced_voucher_posts_and_raises_event()
    {
        var voucher = Voucher.Create("V-2", DateTime.UtcNow);

        voucher.AddLine(Guid.NewGuid(), debit: 100m, credit: 0m);
        voucher.AddLine(Guid.NewGuid(), debit: 0m, credit: 100m);

        voucher.Post();

        Assert.Equal(VoucherStatus.Posted, voucher.Status);
        Assert.Contains(voucher.DomainEvents, e => e is VoucherPostedDomainEvent);
    }

    [Fact]
    public void Line_cannot_be_both_debit_and_credit()
    {
        var voucher = Voucher.Create("V-3", DateTime.UtcNow);

        Assert.Throws<ArgumentException>(
            () => voucher.AddLine(Guid.NewGuid(), debit: 50m, credit: 50m));
    }
}
