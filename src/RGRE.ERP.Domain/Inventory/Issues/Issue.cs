using RGRE.ERP.Domain.Common;
using RGRE.ERP.Domain.Inventory.Issues.Events;
using RGRE.ERP.Domain.Inventory.Receipts;

namespace RGRE.ERP.Domain.Inventory.Issues;

/// <summary>
/// Warehouse-out document. Replaces legacy <c>Khorooj</c> + <c>KhoroojDetail</c>
/// (and the <c>HavaleMasraf</c> consumption flow when used with a cost center).
/// </summary>
public sealed class Issue : AuditableEntity, IDimensionedDocument
{
    private readonly List<IssueLine> _lines = new();
    private readonly List<object> _domainEvents = new();

    public IReadOnlyCollection<object> DomainEvents => _domainEvents.AsReadOnly();

    public Guid Id { get; private set; }

    public string DocumentNumber { get; private set; } = null!;

    public DateTime DocumentDate { get; private set; }

    public Guid WarehouseId { get; private set; }

    /// <summary>Customer / receiver partner (legacy receiver account on Khorooj).</summary>
    public Guid? ReceiverPartnerId { get; private set; }

    /// <summary>Cost center for consumption issues (legacy HavaleMasraf).</summary>
    public Guid? CostCenterId { get; private set; }

    public IssueStatus Status { get; private set; }

    public string? Description { get; private set; }

    /// <summary>Company dimension (legacy <c>CoID</c>).</summary>
    public Guid? CompanyId { get; private set; }

    /// <summary>Branch dimension (legacy <c>BrID</c>).</summary>
    public Guid? BranchId { get; private set; }

    /// <summary>Fiscal year this document belongs to (legacy <c>FyID</c>).</summary>
    public Guid? FiscalYearId { get; private set; }

    public IReadOnlyCollection<IssueLine> Lines => _lines.AsReadOnly();

    private Issue()
    {
    }

    public Issue(
        string documentNumber,
        DateTime documentDate,
        Guid warehouseId,
        Guid? receiverPartnerId = null,
        Guid? costCenterId = null,
        string? description = null)
    {
        if (string.IsNullOrWhiteSpace(documentNumber))
            throw new ArgumentException("Document number is required.");

        if (warehouseId == Guid.Empty)
            throw new ArgumentException("Warehouse is required.");

        Id = Guid.NewGuid();
        DocumentNumber = documentNumber;
        DocumentDate = documentDate;
        WarehouseId = warehouseId;
        ReceiverPartnerId = receiverPartnerId;
        CostCenterId = costCenterId;
        Description = description;
        Status = IssueStatus.Draft;
    }

    public void AddLine(
        Guid goodsId,
        Guid unitId,
        decimal quantity,
        decimal unitPrice,
        string? description = null)
    {
        EnsureDraft();

        if (quantity <= 0)
            throw new ArgumentException("Quantity must be greater than zero.");

        if (unitPrice < 0)
            throw new ArgumentException("Unit price cannot be negative.");

        _lines.Add(new IssueLine(Id, goodsId, unitId, quantity, unitPrice, description));
    }

    public void RemoveLine(Guid lineId)
    {
        EnsureDraft();

        var line = _lines.FirstOrDefault(l => l.Id == lineId)
            ?? throw new InvalidOperationException("Issue line was not found.");

        _lines.Remove(line);
    }

    public decimal GetTotalAmount() => _lines.Sum(l => l.Amount);

    /// <summary>Assigns the company/branch/fiscal-year context; only while Draft.</summary>
    public void AssignContext(Guid? companyId, Guid? branchId, Guid? fiscalYearId)
    {
        EnsureDraft();

        CompanyId = companyId;
        BranchId = branchId;
        FiscalYearId = fiscalYearId;
    }

    /// <summary>Posts the issue; stock is decremented by the InventoryPolicy on save.</summary>
    public void Post()
    {
        EnsureDraft();

        if (_lines.Count == 0)
            throw new InvalidOperationException("Issue must contain at least one line.");

        Status = IssueStatus.Posted;
        _domainEvents.Add(new IssuePostedDomainEvent(
            Id,
            DocumentNumber,
            DocumentDate,
            WarehouseId,
            GetTotalAmount()));
    }

    public void Cancel()
    {
        if (Status != IssueStatus.Posted)
            throw new InvalidOperationException("Only a posted issue can be cancelled.");

        Status = IssueStatus.Cancelled;
    }

    private void EnsureDraft()
    {
        if (Status != IssueStatus.Draft)
            throw new InvalidOperationException(
                "Issue can only be modified while it is in Draft state.");
    }
}

public sealed class IssueLine
{
    public Guid Id { get; private set; }

    public Guid IssueId { get; private set; }

    public Guid GoodsId { get; private set; }

    public Guid UnitId { get; private set; }

    public decimal Quantity { get; private set; }

    public decimal UnitPrice { get; private set; }

    public decimal Amount { get; private set; }

    public string? Description { get; private set; }

    private IssueLine()
    {
    }

    internal IssueLine(
        Guid issueId,
        Guid goodsId,
        Guid unitId,
        decimal quantity,
        decimal unitPrice,
        string? description)
    {
        Id = Guid.NewGuid();
        IssueId = issueId;
        GoodsId = goodsId;
        UnitId = unitId;
        Quantity = quantity;
        UnitPrice = unitPrice;
        Amount = quantity * unitPrice;
        Description = description;
    }
}
