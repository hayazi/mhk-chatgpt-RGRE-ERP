using RGRE.ERP.Domain.Common;

namespace RGRE.ERP.Domain.Accounting;

/// <summary>
/// A ledger account in the chart of accounts (code hierarchy). Replaces the
/// legacy <c>Account</c> hierarchy (Kol / Moin / Tafzili levels).
/// </summary>
public sealed class LedgerAccount : AuditableEntity
{
    public Guid Id { get; private set; }

    /// <summary>Full hierarchical code, e.g. "1101" or "110101001".</summary>
    public string Code { get; private set; } = null!;

    public string Name { get; private set; } = null!;

    /// <summary>Legacy <c>AccLevel</c> semantics: 1 = group (kol), 2 = controlling (moin), 3+ = subsidiary (tafzili).</summary>
    public int Level { get; private set; }

    public Guid? ParentId { get; private set; }

    /// <summary>Legacy <c>BusProLoss</c>: true when the account belongs to the income statement.</summary>
    public bool IsProfitAndLoss { get; private set; }

    /// <summary>True for debit-nature accounts (legacy <c>CreditType</c>).</summary>
    public bool IsDebitNature { get; private set; }

    /// <summary>Legacy <c>AccConstraints</c>: leaf accounts only may be posted to.</summary>
    public bool AllowPosting { get; private set; }

    public bool Enabled { get; private set; }

    private LedgerAccount()
    {
    }

    public LedgerAccount(
        string code,
        string name,
        int level,
        Guid? parentId = null,
        bool isProfitAndLoss = false,
        bool isDebitNature = true,
        bool allowPosting = true)
    {
        if (string.IsNullOrWhiteSpace(code))
            throw new ArgumentException("Account code is required.");

        if (string.IsNullOrWhiteSpace(name))
            throw new ArgumentException("Account name is required.");

        if (level < 1 || level > 5)
            throw new ArgumentException("Account level must be between 1 and 5.");

        if (parentId.HasValue && parentId.Value == Guid.Empty)
            parentId = null;

        Id = Guid.NewGuid();
        Code = code.Trim();
        Name = name.Trim();
        Level = level;
        ParentId = parentId;
        IsProfitAndLoss = isProfitAndLoss;
        IsDebitNature = isDebitNature;
        AllowPosting = allowPosting;
        Enabled = true;
    }

    public void Rename(string name)
    {
        if (string.IsNullOrWhiteSpace(name))
            throw new ArgumentException("Account name is required.");

        Name = name.Trim();
    }

    public void SetPostingAllowed(bool allowed) => AllowPosting = allowed;

    public void Disable() => Enabled = false;
}
