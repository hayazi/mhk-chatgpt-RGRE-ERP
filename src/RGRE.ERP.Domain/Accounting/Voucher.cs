using RGRE.ERP.Domain.Common;

namespace RGRE.ERP.Domain.Accounting;

/// <summary>
/// A journal entry. Replaces the legacy <c>Voucher</c>/<c>Voucherms</c> +
/// <c>VoucherCurrency</c> pair with a single header + lines aggregate.
/// </summary>
public sealed class Voucher : AuditableEntity, IDimensionedDocument
{
    private readonly List<VoucherLine> _lines = new();
    private readonly List<object> _domainEvents = new();

    public IReadOnlyCollection<object> DomainEvents => _domainEvents.AsReadOnly();

    public Guid Id { get; private set; }

    /// <summary>Legacy sequential voucher number (<c>VoucherID</c>).</summary>
    public string Number { get; private set; } = null!;

    public DateTime Date { get; private set; }

    /// <summary>Legacy <c>VchrCategories</c> category id.</summary>
    public Guid? CategoryId { get; private set; }

    public string? Description { get; private set; }

    /// <summary>Company dimension (legacy <c>CoID</c>).</summary>
    public Guid? CompanyId { get; private set; }

    /// <summary>Branch dimension (legacy <c>BrID</c>).</summary>
    public Guid? BranchId { get; private set; }

    /// <summary>Fiscal year this voucher belongs to (legacy <c>FyID</c>).</summary>
    public Guid? FiscalYearId { get; private set; }

    public VoucherStatus Status { get; private set; }

    public IReadOnlyCollection<VoucherLine> Lines => _lines.AsReadOnly();

    private Voucher()
    {
    }

    private Voucher(string number, DateTime date, Guid? categoryId, string? description)
    {
        if (string.IsNullOrWhiteSpace(number))
            throw new ArgumentException("Voucher number is required.");

        Id = Guid.NewGuid();
        Number = number.Trim();
        Date = date;
        CategoryId = categoryId;
        Description = description;
        Status = VoucherStatus.Draft;
    }

    public static Voucher Create(
        string number,
        DateTime date,
        Guid? categoryId = null,
        string? description = null)
        => new(number, date, categoryId, description);

    public VoucherLine AddLine(
        Guid accountId,
        decimal debit,
        decimal credit,
        string? description = null,
        Guid? partnerId = null,
        Guid? warehouseId = null)
    {
        EnsureDraft();

        if (debit < 0 || credit < 0)
            throw new ArgumentException("Debit and credit cannot be negative.");

        if (debit > 0 && credit > 0)
            throw new ArgumentException("A line must be debit or credit, not both.");

        if (debit == 0 && credit == 0)
            throw new ArgumentException("A line must have a debit or credit amount.");

        var line = new VoucherLine(accountId, debit, credit, description, partnerId, warehouseId);
        _lines.Add(line);
        return line;
    }

    public void RemoveLine(Guid lineId)
    {
        EnsureDraft();

        var line = _lines.FirstOrDefault(l => l.Id == lineId)
            ?? throw new InvalidOperationException("Voucher line was not found.");

        _lines.Remove(line);
    }

    public decimal GetTotalDebit() => _lines.Sum(l => l.Debit);

    public decimal GetTotalCredit() => _lines.Sum(l => l.Credit);

    public bool IsBalanced() => GetTotalDebit() == GetTotalCredit();

    /// <summary>Business date used for fiscal-year gating (the voucher date).</summary>
    DateTime Common.IDimensionedDocument.DocumentDate => Date;

    /// <summary>Assigns the company/branch/fiscal-year context; only while Draft.</summary>
    public void AssignContext(Guid? companyId, Guid? branchId, Guid? fiscalYearId)
    {
        EnsureDraft();

        CompanyId = companyId;
        BranchId = branchId;
        FiscalYearId = fiscalYearId;
    }

    /// <summary>Posts the voucher; requires at least two lines and balanced totals.</summary>
    public void Post()
    {
        EnsureDraft();

        if (_lines.Count < 2)
            throw new InvalidOperationException("A voucher needs at least two lines.");

        if (!IsBalanced())
            throw new InvalidOperationException(
                $"Voucher is not balanced: debit {GetTotalDebit()} vs credit {GetTotalCredit()}.");

        Status = VoucherStatus.Posted;
        _domainEvents.Add(new VoucherPostedDomainEvent(Id, Number, Date, GetTotalDebit()));
    }

    public void Cancel()
    {
        if (Status != VoucherStatus.Posted)
            throw new InvalidOperationException("Only a posted voucher can be cancelled.");

        Status = VoucherStatus.Cancelled;
        _domainEvents.Add(new VoucherCancelledDomainEvent(Id, Number, Date));
    }

    private void EnsureDraft()
    {
        if (Status != VoucherStatus.Draft)
            throw new InvalidOperationException("Voucher can only be modified while in Draft state.");
    }
}

public enum VoucherStatus
{
    Draft = 0,
    Posted = 1,
    Cancelled = 2,
}

public sealed record VoucherPostedDomainEvent(
    Guid VoucherId,
    string Number,
    DateTime Date,
    decimal Amount);

public sealed record VoucherCancelledDomainEvent(
    Guid VoucherId,
    string Number,
    DateTime Date);
