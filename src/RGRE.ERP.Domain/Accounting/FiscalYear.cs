using RGRE.ERP.Domain.Common;

namespace RGRE.ERP.Domain.Accounting;

/// <summary>
/// Fiscal year (legacy <c>FYear</c>). Every accounting/inventory document
/// belongs to exactly one fiscal year; posting into a non-open year is rejected.
/// </summary>
public sealed class FiscalYear : AuditableEntity
{
    public Guid Id { get; private set; }

    /// <summary>Display name, e.g. "سال مالی ۱۴۰۵" or "FY 2026/27".</summary>
    public string Name { get; private set; } = null!;

    /// <summary>First day of the fiscal year (inclusive, Gregorian).</summary>
    public DateTime StartDate { get; private set; }

    /// <summary>Last day of the fiscal year (inclusive, Gregorian).</summary>
    public DateTime EndDate { get; private set; }

    /// <summary>
    /// Legacy <c>FYear.IsClosed</c>: a closed year rejects new documents and
    /// changes; exactly one year may be open at a time.
    /// </summary>
    public bool IsClosed { get; private set; }

    public Guid CompanyId { get; private set; }

    private FiscalYear()
    {
    }

    public FiscalYear(
        Guid companyId,
        string name,
        DateTime startDate,
        DateTime endDate)
    {
        if (companyId == Guid.Empty)
            throw new ArgumentException("Company is required.");

        if (string.IsNullOrWhiteSpace(name))
            throw new ArgumentException("Fiscal year name is required.");

        if (endDate.Date < startDate.Date)
            throw new ArgumentException("Fiscal year end date must not be before its start date.");

        Id = Guid.NewGuid();
        CompanyId = companyId;
        Name = name.Trim();
        StartDate = startDate.Date;
        EndDate = endDate.Date;
        IsClosed = false;
    }

    /// <summary>True when the date falls inside the year's date range.</summary>
    public bool Contains(DateTime date)
    {
        var d = date.Date;
        return StartDate <= d && d <= EndDate;
    }

    public void Close()
    {
        if (IsClosed)
            throw new InvalidOperationException("Fiscal year is already closed.");

        IsClosed = true;
    }

    public void Reopen() => IsClosed = false;

    /// <summary>Domain rule: a document can only be posted into an open fiscal year it belongs to.</summary>
    public void EnsureAcceptsPosting(DateTime documentDate)
    {
        if (IsClosed)
            throw new InvalidOperationException(
                $"Fiscal year '{Name}' is closed; posting is not allowed.");

        if (!Contains(documentDate))
            throw new InvalidOperationException(
                $"Document date {documentDate:yyyy-MM-dd} is outside fiscal year '{Name}' " +
                $"({StartDate:yyyy-MM-dd} .. {EndDate:yyyy-MM-dd}).");
    }
}

public interface IFiscalYearRepository
{
    Task<FiscalYear?> GetAsync(Guid id, CancellationToken cancellationToken = default);

    Task<FiscalYear?> GetOpenAsync(CancellationToken cancellationToken = default);

    Task<IReadOnlyList<FiscalYear>> ListAsync(CancellationToken cancellationToken = default);

    Task AddAsync(FiscalYear fiscalYear, CancellationToken cancellationToken = default);
}
