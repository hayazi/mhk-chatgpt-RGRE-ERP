using RGRE.ERP.Domain.Common;
using RGRE.ERP.Domain.Inventory.Receipts.Events;

namespace RGRE.ERP.Domain.Inventory.Receipts;

/// <summary>
/// Warehouse-in document. Replaces the legacy <c>Resid</c> header + <c>ResidDetail</c> rows.
/// </summary>
public sealed class Receipt : AuditableEntity, IDimensionedDocument
{
    private readonly List<ReceiptLine> _lines = new();
    private readonly List<object> _domainEvents = new();

    public IReadOnlyCollection<object> DomainEvents => _domainEvents.AsReadOnly();

    public Guid Id { get; private set; }

    public string DocumentNumber { get; private set; } = null!;

    public DateTime DocumentDate { get; private set; }

    public Guid WarehouseId { get; private set; }

    public Guid? SupplierId { get; private set; }

    public ReceiptStatus Status { get; private set; }

    public string? Description { get; private set; }

    /// <summary>Company dimension (legacy <c>CoID</c>).</summary>
    public Guid? CompanyId { get; private set; }

    /// <summary>Branch dimension (legacy <c>BrID</c>).</summary>
    public Guid? BranchId { get; private set; }

    /// <summary>Fiscal year this document belongs to (legacy <c>FyID</c>).</summary>
    public Guid? FiscalYearId { get; private set; }

    /// <summary>Legacy row-order field (<c>Radif</c>).</summary>
    public int LineNumber { get; private set; }

    public IReadOnlyCollection<ReceiptLine> Lines => _lines.AsReadOnly();

    private Receipt()
    {
    }

    private Receipt(
        string documentNumber,
        DateTime documentDate,
        Guid warehouseId,
        Guid? supplierId,
        string? description)
    {
        if (string.IsNullOrWhiteSpace(documentNumber))
            throw new ArgumentException("Document number is required.");

        if (warehouseId == Guid.Empty)
            throw new ArgumentException("Warehouse is required.");

        Id = Guid.NewGuid();
        DocumentNumber = documentNumber;
        DocumentDate = documentDate;
        WarehouseId = warehouseId;
        SupplierId = supplierId;
        Description = description;
        Status = ReceiptStatus.Draft;
    }

    public static Receipt Create(
        string documentNumber,
        DateTime documentDate,
        Guid warehouseId,
        Guid? supplierId = null,
        string? description = null)
    {
        return new Receipt(
            documentNumber,
            documentDate,
            warehouseId,
            supplierId,
            description);
    }

    public void AddLine(
        Guid productId,
        Guid unitId,
        decimal quantity,
        decimal unitPrice,
        string? description = null)
    {
        EnsureDraft();

        var line = new ReceiptLine(
            Id,
            productId,
            unitId,
            quantity,
            unitPrice,
            description);

        _lines.Add(line);
    }

    public void RemoveLine(Guid lineId)
    {
        EnsureDraft();

        var line = _lines.FirstOrDefault(x => x.Id == lineId);

        if (line is null)
            throw new InvalidOperationException("Receipt line was not found.");

        _lines.Remove(line);
        RenumberLines();
    }

    private void RenumberLines()
    {
        LineNumber = 0;
        foreach (var line in _lines)
        {
            LineNumber++;
        }
    }

    public decimal GetTotalAmount()
    {
        return _lines.Sum(x => x.Amount);
    }

    /// <summary>Assigns the company/branch/fiscal-year context; only while Draft.</summary>
    public void AssignContext(Guid? companyId, Guid? branchId, Guid? fiscalYearId)
    {
        EnsureDraft();

        CompanyId = companyId;
        BranchId = branchId;
        FiscalYearId = fiscalYearId;
    }

    public void Post()
    {
        EnsureDraft();

        if (_lines.Count == 0)
            throw new InvalidOperationException(
                "Receipt must contain at least one line.");

        Status = ReceiptStatus.Posted;
        _domainEvents.Add(
            new ReceiptPostedDomainEvent(
                Id,
                DocumentNumber,
                DocumentDate,
                WarehouseId,
                GetTotalAmount()));
    }

    public void Cancel()
    {
        if (Status != ReceiptStatus.Posted)
            throw new InvalidOperationException(
                "Only a posted receipt can be cancelled.");

        Status = ReceiptStatus.Cancelled;
    }

    private void EnsureDraft()
    {
        if (Status != ReceiptStatus.Draft)
            throw new InvalidOperationException(
                "Receipt can only be modified while it is in Draft state.");
    }
}
