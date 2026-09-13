using RGRE.ERP.Application.Abstractions;
using RGRE.ERP.Domain.Accounting;
using RGRE.ERP.Domain.Common;

namespace RGRE.ERP.Application.Platform;

/// <summary>
/// Cross-cutting rules applied to every document created or posted through
/// the application services: stamp the user's company/branch/fiscal-year
/// context on creation and reject posting into a closed/out-of-range year.
/// </summary>
public static class DocumentContextRules
{
    /// <summary>Stamps the working context onto a newly created document.</summary>
    public static async Task StampCreationContextAsync(
        IFiscalYearRepository fiscalYears,
        ICurrentUserContext userContext,
        IDimensionedDocument document,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(fiscalYears);
        ArgumentNullException.ThrowIfNull(userContext);
        ArgumentNullException.ThrowIfNull(document);

        var openYear = await fiscalYears.GetOpenAsync(cancellationToken);

        document.AssignContext(
            userContext.CompanyId,
            userContext.BranchId,
            openYear?.Id ?? document.FiscalYearId);
    }

    /// <summary>
    /// Gates posting: when an open fiscal year exists the document date must
    /// fall inside it; a document without a fiscal year yet is stamped with
    /// the open year. When no fiscal year exists at all (fresh databases,
    /// tests) posting is unrestricted.
    /// </summary>
    public static async Task EnsureFiscalYearAcceptsAsync(
        IFiscalYearRepository fiscalYears,
        ICurrentUserContext userContext,
        IDimensionedDocument document,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(fiscalYears);
        ArgumentNullException.ThrowIfNull(userContext);
        ArgumentNullException.ThrowIfNull(document);

        var openYear = await fiscalYears.GetOpenAsync(cancellationToken);

        if (openYear is null)
            return;

        openYear.EnsureAcceptsPosting(document.DocumentDate);

        if (document.FiscalYearId is null)
        {
            document.AssignContext(
                document.CompanyId ?? userContext.CompanyId,
                document.BranchId ?? userContext.BranchId,
                openYear.Id);
        }
    }

    /// <summary>
    /// Replaces the stored dimensions with the caller's working context;
    /// used by the context-switch endpoint to re-stamp draft documents.
    /// </summary>
    public static void RestampContext(
        ICurrentUserContext userContext,
        IDimensionedDocument document,
        Guid fiscalYearId)
    {
        ArgumentNullException.ThrowIfNull(userContext);
        ArgumentNullException.ThrowIfNull(document);

        document.AssignContext(userContext.CompanyId, userContext.BranchId, fiscalYearId);
    }
}
