namespace RGRE.ERP.Domain.Common;

/// <summary>
/// A business document that carries the three context dimensions
/// (company / branch / fiscal year) stamped from the user's working context.
/// </summary>
public interface IDimensionedDocument
{
    Guid? CompanyId { get; }

    Guid? BranchId { get; }

    Guid? FiscalYearId { get; }

    /// <summary>Business date used for fiscal-year gating.</summary>
    DateTime DocumentDate { get; }

    /// <summary>Assigns the context dimensions; must only work in Draft state.</summary>
    void AssignContext(Guid? companyId, Guid? branchId, Guid? fiscalYearId);
}
