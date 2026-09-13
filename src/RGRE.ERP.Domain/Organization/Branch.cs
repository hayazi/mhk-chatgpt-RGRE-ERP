using RGRE.ERP.Domain.Common;

namespace RGRE.ERP.Domain.Organization;

/// <summary>
/// A branch of a company. Replaces the legacy <c>Branchs</c> table;
/// business documents are scoped to a branch and the branch dimension
/// gates which rows a user works with.
/// </summary>
public sealed class Branch : AuditableEntity
{
    public Guid Id { get; private set; }

    public Guid CompanyId { get; private set; }

    /// <summary>Short code, unique within the company.</summary>
    public string Code { get; private set; } = null!;

    public string Name { get; private set; } = null!;

    public string? Address { get; private set; }

    public string? Telephone { get; private set; }

    public bool Enabled { get; private set; }

    private Branch()
    {
    }

    public Branch(
        Guid companyId,
        string code,
        string name,
        string? address = null,
        string? telephone = null)
    {
        if (companyId == Guid.Empty)
            throw new ArgumentException("Company is required.");

        if (string.IsNullOrWhiteSpace(code))
            throw new ArgumentException("Branch code is required.");

        if (string.IsNullOrWhiteSpace(name))
            throw new ArgumentException("Branch name is required.");

        Id = Guid.NewGuid();
        CompanyId = companyId;
        Code = code.Trim();
        Name = name.Trim();
        Address = address;
        Telephone = telephone;
        Enabled = true;
    }

    public void Rename(string name)
    {
        if (string.IsNullOrWhiteSpace(name))
            throw new ArgumentException("Branch name is required.");

        Name = name.Trim();
    }

    public void SetContact(string? address, string? telephone)
    {
        Address = address;
        Telephone = telephone;
    }

    public void Disable() => Enabled = false;

    public void Enable() => Enabled = true;
}
