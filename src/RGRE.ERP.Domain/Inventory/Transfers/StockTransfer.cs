using RGRE.ERP.Domain.Common;
using RGRE.ERP.Domain.Inventory.Receipts;
using RGRE.ERP.Domain.Inventory.Transfers.Events;

namespace RGRE.ERP.Domain.Inventory.Transfers;

/// <summary>
/// Transfer between two warehouses. Replaces the legacy
/// <c>EnteghalBeinAnbar</c> + <c>EnteghalBeinAnbarDetail</c>.
/// </summary>
public sealed class StockTransfer : AuditableEntity, IDimensionedDocument
{
    private readonly List<StockTransferLine> _lines = new();
    private readonly List<object> _domainEvents = new();

    public IReadOnlyCollection<object> DomainEvents => _domainEvents.AsReadOnly();

    public Guid Id { get; private set; }

    public string DocumentNumber { get; private set; } = null!;

    public DateTime DocumentDate { get; private set; }

    public Guid FromWarehouseId { get; private set; }

    public Guid ToWarehouseId { get; private set; }

    public TransferStatus Status { get; private set; }

    public string? Description { get; private set; }

    /// <summary>Company dimension (legacy <c>CoID</c>).</summary>
    public Guid? CompanyId { get; private set; }

    /// <summary>Branch dimension (legacy <c>BrID</c>).</summary>
    public Guid? BranchId { get; private set; }

    /// <summary>Fiscal year this document belongs to (legacy <c>FyID</c>).</summary>
    public Guid? FiscalYearId { get; private set; }

    public IReadOnlyCollection<StockTransferLine> Lines => _lines.AsReadOnly();

    private StockTransfer()
    {
    }

    public StockTransfer(
        string documentNumber,
        DateTime documentDate,
        Guid fromWarehouseId,
        Guid toWarehouseId,
        string? description = null)
    {
        if (string.IsNullOrWhiteSpace(documentNumber))
            throw new ArgumentException("Document number is required.");

        if (fromWarehouseId == Guid.Empty || toWarehouseId == Guid.Empty)
            throw new ArgumentException("Both warehouses are required.");

        if (fromWarehouseId == toWarehouseId)
            throw new ArgumentException("Source and destination warehouses must differ.");

        Id = Guid.NewGuid();
        DocumentNumber = documentNumber;
        DocumentDate = documentDate;
        FromWarehouseId = fromWarehouseId;
        ToWarehouseId = toWarehouseId;
        Description = description;
        Status = TransferStatus.Draft;
    }

    public void AddLine(Guid goodsId, Guid unitId, decimal quantity, string? description = null)
    {
        EnsureDraft();

        if (quantity <= 0)
            throw new ArgumentException("Quantity must be greater than zero.");

        _lines.Add(new StockTransferLine(Id, goodsId, unitId, quantity, description));
    }

    public void RemoveLine(Guid lineId)
    {
        EnsureDraft();

        var line = _lines.FirstOrDefault(l => l.Id == lineId)
            ?? throw new InvalidOperationException("Transfer line was not found.");

        _lines.Remove(line);
    }

    public decimal GetTotalQuantity() => _lines.Sum(l => l.Quantity);

    /// <summary>Assigns the company/branch/fiscal-year context; only while Draft.</summary>
    public void AssignContext(Guid? companyId, Guid? branchId, Guid? fiscalYearId)
    {
        EnsureDraft();

        CompanyId = companyId;
        BranchId = branchId;
        FiscalYearId = fiscalYearId;
    }

    /// <summary>
    /// Posts the transfer. Creates Out moves for the source warehouse and In
    /// moves for the destination warehouse on the same goods.
    /// </summary>
    public void Post()
    {
        EnsureDraft();

        if (_lines.Count == 0)
            throw new InvalidOperationException("Transfer must contain at least one line.");

        Status = TransferStatus.Posted;
        _domainEvents.Add(new StockTransferredDomainEvent(
            Id,
            DocumentNumber,
            DocumentDate,
            FromWarehouseId,
            ToWarehouseId,
            _lines.Count));
    }

    public void Cancel()
    {
        if (Status != TransferStatus.Posted)
            throw new InvalidOperationException("Only a posted transfer can be cancelled.");

        Status = TransferStatus.Cancelled;
    }

    private void EnsureDraft()
    {
        if (Status != TransferStatus.Draft)
            throw new InvalidOperationException(
                "Transfer can only be modified while it is in Draft state.");
    }
}

public sealed class StockTransferLine
{
    public Guid Id { get; private set; }

    public Guid TransferId { get; private set; }

    public Guid GoodsId { get; private set; }

    public Guid UnitId { get; private set; }

    public decimal Quantity { get; private set; }

    public string? Description { get; private set; }

    private StockTransferLine()
    {
    }

    internal StockTransferLine(
        Guid transferId,
        Guid goodsId,
        Guid unitId,
        decimal quantity,
        string? description)
    {
        Id = Guid.NewGuid();
        TransferId = transferId;
        GoodsId = goodsId;
        UnitId = unitId;
        Quantity = quantity;
        Description = description;
    }
}
