using RGRE.ERP.Application.Abstractions;
using RGRE.ERP.Domain.Inventory;
using RGRE.ERP.Domain.Inventory.Issues;
using RGRE.ERP.Domain.Inventory.Receipts;
using RGRE.ERP.Domain.Inventory.Transfers;

namespace RGRE.ERP.Application.Inventory;

/// <summary>
/// Turns posted inventory documents into <see cref="StockMove"/> ledger rows
/// and keeps <see cref="StockBalance"/> up to date. This is the RGRE-ERP
/// replacement for the legacy <c>Analyze</c> tables.
/// </summary>
public interface IStockLedgerService
{
    Task ApplyReceiptAsync(Receipt receipt, CancellationToken cancellationToken = default);

    Task ApplyIssueAsync(Issue issue, CancellationToken cancellationToken = default);

    Task ApplyTransferAsync(StockTransfer transfer, CancellationToken cancellationToken = default);
}

public sealed class StockLedgerService : IStockLedgerService
{
    private readonly IStockMoveRepository _moves;
    private readonly IStockBalanceRepository _balances;
    private readonly IUnitRepository _units;

    public StockLedgerService(
        IStockMoveRepository moves,
        IStockBalanceRepository balances,
        IUnitRepository units)
    {
        _moves = moves;
        _balances = balances;
        _units = units;
    }

    public async Task ApplyReceiptAsync(
        Receipt receipt,
        CancellationToken cancellationToken = default)
    {
        foreach (var line in receipt.Lines)
        {
            await ApplyAsync(
                documentId: receipt.Id,
                direction: StockMoveDirection.In,
                moveDate: receipt.DocumentDate,
                warehouseId: receipt.WarehouseId,
                goodsId: line.ProductId,
                unitId: line.UnitId,
                quantity: line.Quantity,
                unitPrice: line.UnitPrice,
                description: line.Description,
                cancellationToken: cancellationToken);
        }
    }

    public async Task ApplyIssueAsync(
        Issue issue,
        CancellationToken cancellationToken = default)
    {
        foreach (var line in issue.Lines)
        {
            await ApplyAsync(
                documentId: issue.Id,
                direction: StockMoveDirection.Out,
                moveDate: issue.DocumentDate,
                warehouseId: issue.WarehouseId,
                goodsId: line.GoodsId,
                unitId: line.UnitId,
                quantity: line.Quantity,
                unitPrice: line.UnitPrice,
                description: line.Description,
                cancellationToken: cancellationToken);
        }
    }

    public async Task ApplyTransferAsync(
        StockTransfer transfer,
        CancellationToken cancellationToken = default)
    {
        foreach (var line in transfer.Lines)
        {
            // Outbound from the source warehouse.
            await ApplyAsync(
                documentId: transfer.Id,
                direction: StockMoveDirection.Out,
                moveDate: transfer.DocumentDate,
                warehouseId: transfer.FromWarehouseId,
                goodsId: line.GoodsId,
                unitId: line.UnitId,
                quantity: line.Quantity,
                unitPrice: 0m,
                description: line.Description,
                cancellationToken: cancellationToken);

            // Inbound to the destination warehouse.
            await ApplyAsync(
                documentId: transfer.Id,
                direction: StockMoveDirection.In,
                moveDate: transfer.DocumentDate,
                warehouseId: transfer.ToWarehouseId,
                goodsId: line.GoodsId,
                unitId: line.UnitId,
                quantity: line.Quantity,
                unitPrice: 0m,
                description: line.Description,
                cancellationToken: cancellationToken);
        }
    }

    private async Task ApplyAsync(
        Guid documentId,
        StockMoveDirection direction,
        DateTime moveDate,
        Guid warehouseId,
        Guid goodsId,
        Guid unitId,
        decimal quantity,
        decimal unitPrice,
        string? description,
        CancellationToken cancellationToken)
    {
        var baseQuantity = await ToBaseQuantityAsync(goodsId, unitId, quantity, cancellationToken);

        var move = new StockMove(
            documentId,
            direction,
            moveDate,
            warehouseId,
            goodsId,
            unitId,
            quantity,
            unitPrice,
            description);

        move.SetBaseQuantity(baseQuantity);
        await _moves.AddRangeAsync(new[] { move }, cancellationToken);

        var balance = await _balances.GetAsync(warehouseId, goodsId, cancellationToken)
            ?? new StockBalance(warehouseId, goodsId);

        balance.Apply(
            direction == StockMoveDirection.In ? baseQuantity : -baseQuantity,
            baseQuantity * unitPrice);

        await _balances.AddAsync(balance, cancellationToken);
    }

    private async Task<decimal> ToBaseQuantityAsync(
        Guid goodsId,
        Guid unitId,
        decimal quantity,
        CancellationToken cancellationToken)
    {
        var unit = await _units.GetAsync(unitId, cancellationToken);

        if (unit is null)
            return quantity; // assume the line already uses the base unit

        return quantity * unit.ConversionFactor;
    }
}
