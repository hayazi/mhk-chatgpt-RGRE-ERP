using RGRE.ERP.Domain.Common;

namespace RGRE.ERP.Domain.Partners;

/// <summary>
/// A business partner: customer and/or supplier (the legacy split of
/// <c>Customer</c> and supplier accounts in <c>Account</c>).
/// </summary>
public sealed class Partner : AuditableEntity
{
    public Guid Id { get; private set; }

    /// <summary>Legacy <c>Accountcd</c> style business code.</summary>
    public string Code { get; private set; } = null!;

    public string Name { get; private set; } = null!;

    /// <summary>Legacy <c>Accountnm2</c>.</summary>
    public string? Name2 { get; private set; }

    public PartnerKind Kind { get; private set; }

    public string? NationalId { get; private set; }

    public string? EconomicCode { get; private set; }

    public string? Telephone { get; private set; }

    public string? Email { get; private set; }

    public string? Address { get; private set; }

    /// <summary>Legacy <c>SaghfEtebariNaghdi</c>.</summary>
    public decimal? CashCreditLimit { get; private set; }

    /// <summary>Legacy <c>SaghfEtebariCheki</c>.</summary>
    public decimal? ChequeCreditLimit { get; private set; }

    /// <summary>Legacy <c>MamnoMoamele</c> - transaction blocked flag.</summary>
    public bool IsBlocked { get; private set; }

    public string? Description { get; private set; }

    private Partner()
    {
    }

    public Partner(
        string code,
        string name,
        PartnerKind kind,
        string? name2 = null,
        string? nationalId = null,
        string? economicCode = null,
        string? telephone = null,
        string? email = null,
        string? address = null,
        decimal? cashCreditLimit = null,
        decimal? chequeCreditLimit = null,
        string? description = null)
    {
        if (string.IsNullOrWhiteSpace(code))
            throw new ArgumentException("Partner code is required.");

        if (string.IsNullOrWhiteSpace(name))
            throw new ArgumentException("Partner name is required.");

        if (kind == PartnerKind.None)
            throw new ArgumentException("Partner kind must be customer and/or supplier.");

        Id = Guid.NewGuid();
        Code = code.Trim();
        Name = name.Trim();
        Kind = kind;
        Name2 = name2;
        NationalId = nationalId;
        EconomicCode = economicCode;
        Telephone = telephone;
        Email = email;
        Address = address;
        CashCreditLimit = cashCreditLimit;
        ChequeCreditLimit = chequeCreditLimit;
        Description = description;
    }

    public void Rename(string name, string? name2 = null)
    {
        if (string.IsNullOrWhiteSpace(name))
            throw new ArgumentException("Partner name is required.");

        Name = name.Trim();
        Name2 = name2;
    }

    public void SetContact(string? telephone, string? email, string? address)
    {
        Telephone = telephone;
        Email = email;
        Address = address;
    }

    public void Block() => IsBlocked = true;

    public void Unblock() => IsBlocked = false;

    public void SetCreditLimits(decimal? cash, decimal? cheque)
    {
        if (cash.HasValue && cash.Value < 0)
            throw new ArgumentException("Cash credit limit cannot be negative.");

        if (cheque.HasValue && cheque.Value < 0)
            throw new ArgumentException("Cheque credit limit cannot be negative.");

        CashCreditLimit = cash;
        ChequeCreditLimit = cheque;
    }
}

[Flags]
public enum PartnerKind
{
    None = 0,
    Customer = 1,
    Supplier = 2,
}
