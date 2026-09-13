using Xunit;
using RGRE.ERP.Application.Inventory;
using RGRE.ERP.Application.Inventory.Receipts;
using RGRE.ERP.Application.Platform;
using RGRE.ERP.Domain.Accounting;
using RGRE.ERP.Domain.Common;
using RGRE.ERP.Domain.Inventory;
using RGRE.ERP.Domain.Inventory.Receipts;

namespace RGRE.ERP.Application.Tests;

public class ReceiptFlowTests
{
    private static (ReceiptAppService Service, TestHarness Harness) CreateService()
    {
        var harness = new TestHarness();
        var service = new ReceiptAppService(
            harness.Receipts,
            harness.Goods,
            harness.Ledger,
            harness.UnitOfWork,
            harness.FiscalYears,
            harness.User);
        return (service, harness);
    }

    private sealed class TestHarness
    {
        public InMemoryReceiptRepository Receipts { get; } = new();
        public InMemoryGoodsRepository Goods { get; } = new();
        public InMemoryUnitRepository Units { get; } = new();
        public InMemoryStockMoveRepository Moves { get; } = new();
        public InMemoryStockBalanceRepository Balances { get; } = new();
        public InMemoryUnitOfWork UnitOfWork { get; } = new();
        public InMemoryFiscalYearRepository FiscalYears { get; } = new();
        public StaticUserContext User { get; } = StaticUserContext.Anonymous;

        public StockLedgerService Ledger { get; }

        public TestHarness()
        {
            Ledger = new StockLedgerService(Moves, Balances, Units);
        }
    }

    [Fact]
    public async Task Create_with_lines_persists_draft_receipt()
    {
        var (service, harness) = CreateService();
        var goods = new Goods("G1", "Test goods", Guid.NewGuid());
        await harness.Goods.AddAsync(goods);

        var receiptId = await service.CreateAsync(new CreateReceiptDto
        {
            DocumentNumber = "R-100",
            DocumentDate = DateTime.UtcNow,
            WarehouseId = Guid.NewGuid(),
            Lines =
            {
                new AddReceiptLineDto
                {
                    GoodsId = goods.Id,
                    UnitId = goods.BaseUnitId,
                    Quantity = 10,
                    UnitPrice = 1000m,
                },
            },
        });

        var receipt = harness.Receipts.Store[receiptId];
        Assert.Equal(ReceiptStatus.Draft, receipt.Status);
        Assert.Single(receipt.Lines);
        Assert.Equal(10_000m, receipt.GetTotalAmount());
    }

    [Fact]
    public async Task Post_creates_stock_moves_and_updates_balance()
    {
        var (service, harness) = CreateService();
        var goods = new Goods("G1", "Test goods", Guid.NewGuid());
        await harness.Goods.AddAsync(goods);

        var warehouseId = Guid.NewGuid();
        var receiptId = await service.CreateAsync(new CreateReceiptDto
        {
            DocumentNumber = "R-101",
            DocumentDate = DateTime.UtcNow,
            WarehouseId = warehouseId,
            Lines =
            {
                new AddReceiptLineDto { GoodsId = goods.Id, UnitId = goods.BaseUnitId, Quantity = 5, UnitPrice = 200m },
                new AddReceiptLineDto { GoodsId = goods.Id, UnitId = goods.BaseUnitId, Quantity = 3, UnitPrice = 100m },
            },
        });

        await service.PostAsync(receiptId);

        var receipt = harness.Receipts.Store[receiptId];
        Assert.Equal(ReceiptStatus.Posted, receipt.Status);

        Assert.Equal(2, harness.Moves.Moves.Count);
        Assert.All(harness.Moves.Moves, m => Assert.Equal(StockMoveDirection.In, m.Direction));
        Assert.All(harness.Moves.Moves, m => Assert.Equal(goods.Id, m.GoodsId));

        var balance = harness.Balances.Store[(warehouseId, goods.Id)];
        Assert.Equal(8m, balance.Quantity);
        Assert.Equal(5 * 200m + 3 * 100m, balance.TotalValueIn);
    }

    [Fact]
    public async Task Cannot_post_twice()
    {
        var (service, harness) = CreateService();
        var goods = new Goods("G1", "Test goods", Guid.NewGuid());
        await harness.Goods.AddAsync(goods);

        var receiptId = await service.CreateAsync(new CreateReceiptDto
        {
            DocumentNumber = "R-102",
            DocumentDate = DateTime.UtcNow,
            WarehouseId = Guid.NewGuid(),
            Lines =
            {
                new AddReceiptLineDto { GoodsId = goods.Id, UnitId = goods.BaseUnitId, Quantity = 1, UnitPrice = 10m },
            },
        });

        await service.PostAsync(receiptId);

        await Assert.ThrowsAsync<InvalidOperationException>(() => service.PostAsync(receiptId));
    }

    [Fact]
    public async Task Unknown_goods_is_rejected()
    {
        var (service, _) = CreateService();

        await Assert.ThrowsAsync<KeyNotFoundException>(() => service.CreateAsync(new CreateReceiptDto
        {
            DocumentNumber = "R-103",
            DocumentDate = DateTime.UtcNow,
            WarehouseId = Guid.NewGuid(),
            Lines =
            {
                new AddReceiptLineDto { GoodsId = Guid.NewGuid(), UnitId = Guid.NewGuid(), Quantity = 1, UnitPrice = 1m },
            },
        }));
    }

    [Fact]
    public async Task Create_stamps_working_context_on_the_document()
    {
        var (service, harness) = CreateService();
        var goods = new Goods("G1", "Test goods", Guid.NewGuid());
        await harness.Goods.AddAsync(goods);

        var companyId = Guid.NewGuid();
        var branchId = Guid.NewGuid();
        var context = new StaticUserContext
        {
            IsAuthenticated = true,
            UserId = Guid.NewGuid(),
            CompanyId = companyId,
            BranchId = branchId,
        };
        var serviceWithContext = new ReceiptAppService(
            harness.Receipts,
            harness.Goods,
            harness.Ledger,
            harness.UnitOfWork,
            harness.FiscalYears,
            context);

        var receiptId = await serviceWithContext.CreateAsync(new CreateReceiptDto
        {
            DocumentNumber = "R-104",
            DocumentDate = DateTime.UtcNow,
            WarehouseId = Guid.NewGuid(),
            Lines =
            {
                new AddReceiptLineDto { GoodsId = goods.Id, UnitId = goods.BaseUnitId, Quantity = 1, UnitPrice = 1m },
            },
        });

        var receipt = harness.Receipts.Store[receiptId];
        Assert.Equal(companyId, receipt.CompanyId);
        Assert.Equal(branchId, receipt.BranchId);
    }

    [Fact]
    public async Task Posting_outside_the_open_fiscal_year_is_rejected()
    {
        var (service, harness) = CreateService();
        var goods = new Goods("G1", "Test goods", Guid.NewGuid());
        await harness.Goods.AddAsync(goods);

        // Open fiscal year 1405: 21 Mar 2026 .. 20 Mar 2027.
        await harness.FiscalYears.AddAsync(new FiscalYear(
            Guid.NewGuid(),
            "FY 1405",
            JalaliDate.ToGregorian(1405, 1, 1),
            JalaliDate.ToGregorian(1405, 12, 29)));

        var receiptId = await service.CreateAsync(new CreateReceiptDto
        {
            DocumentNumber = "R-105",
            DocumentDate = JalaliDate.ToGregorian(1404, 6, 1), // before the open year
            WarehouseId = Guid.NewGuid(),
            Lines =
            {
                new AddReceiptLineDto { GoodsId = goods.Id, UnitId = goods.BaseUnitId, Quantity = 1, UnitPrice = 1m },
            },
        });

        var exception = await Assert.ThrowsAsync<InvalidOperationException>(() => service.PostAsync(receiptId));

        Assert.Contains("outside fiscal year", exception.Message);
        Assert.Equal(ReceiptStatus.Draft, harness.Receipts.Store[receiptId].Status);
    }

    [Fact]
    public async Task Posting_inside_the_open_fiscal_year_stamps_it_on_the_document()
    {
        var (service, harness) = CreateService();
        var goods = new Goods("G1", "Test goods", Guid.NewGuid());
        await harness.Goods.AddAsync(goods);

        var fy = new FiscalYear(
            Guid.NewGuid(),
            "FY 1405",
            JalaliDate.ToGregorian(1405, 1, 1),
            JalaliDate.ToGregorian(1405, 12, 29));
        await harness.FiscalYears.AddAsync(fy);

        var receiptId = await service.CreateAsync(new CreateReceiptDto
        {
            DocumentNumber = "R-106",
            DocumentDate = JalaliDate.ToGregorian(1405, 6, 1),
            WarehouseId = Guid.NewGuid(),
            Lines =
            {
                new AddReceiptLineDto { GoodsId = goods.Id, UnitId = goods.BaseUnitId, Quantity = 1, UnitPrice = 1m },
            },
        });

        await service.PostAsync(receiptId);

        var receipt = harness.Receipts.Store[receiptId];
        Assert.Equal(ReceiptStatus.Posted, receipt.Status);
        Assert.Equal(fy.Id, receipt.FiscalYearId);
    }
}
