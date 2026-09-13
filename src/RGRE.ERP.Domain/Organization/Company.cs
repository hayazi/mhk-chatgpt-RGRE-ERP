using RGRE.ERP.Domain.Common;

namespace RGRE.ERP.Domain.Organization;

/// <summary>
/// A legal company (database of accounts). Replaces the legacy
/// <c>Corporations</c> table; every business document belongs to one company.
/// </summary>
public sealed class Company : AuditableEntity
{
    public Guid Id { get; private set; }

    /// <summary>Short business code, unique.</summary>
    public string Code { get; private set; } = null!;

    public string Name { get; private set; } = null!;

    /// <summary>Legacy <c>Corporations.Name2</c>.</summary>
    public string? Name2 { get; private set; }

    /// <summary>Legacy <c>EconomicCode</c> (کد اقتصادی).</summary>
    public string? EconomicCode { get; private set; }

    /// <summary>Legacy <c>NationalId</c> (شناسه ملی).</summary>
    public string? NationalId { get; private set; }

    public bool Enabled { get; private set; }

    private Company()
    {
    }

    public Company(
        string code,
        string name,
        string? name2 = null,
        string? economicCode = null,
        string? nationalId = null)
    {
        if (string.IsNullOrWhiteSpace(code))
            throw new ArgumentException("Company code is required.");

        if (string.IsNullOrWhiteSpace(name))
            throw new ArgumentException("Company name is required.");

        Id = Guid.NewGuid();
        Code = code.Trim();
        Name = name.Trim();
        Name2 = name2;
        EconomicCode = economicCode;
        NationalId = nationalId;
        Enabled = true;
    }

    public void Rename(string name, string? name2 = null)
    {
        if (string.IsNullOrWhiteSpace(name))
            throw new ArgumentException("Company name is required.");

        Name = name.Trim();
        Name2 = name2;
    }

    public void SetLegalIdentity(string? economicCode, string? nationalId)
    {
        EconomicCode = economicCode;
        NationalId = nationalId;
    }

    public void Disable() => Enabled = false;

    public void Enable() => Enabled = true;
}
