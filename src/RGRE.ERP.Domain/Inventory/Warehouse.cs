using RGRE.ERP.Domain.Common;

namespace RGRE.ERP.Domain.Inventory;

/// <summary>
/// Warehouse. Replaces the legacy <c>Anbar</c> table (self-referencing
/// <c>ParentID</c> is kept).
/// </summary>
public sealed class Warehouse : AuditableEntity
{
    public Guid Id { get; private set; }

    /// <summary>Legacy 2-char <c>Code</c>.</summary>
    public string Code { get; private set; } = null!;

    public string Name { get; private set; } = null!;

    public Guid? ParentId { get; private set; }

    public string? Address { get; private set; }

    public string? Telephone { get; private set; }

    public string? Description { get; private set; }

    private Warehouse()
    {
    }

    public Warehouse(
        string code,
        string name,
        Guid? parentId = null,
        string? address = null,
        string? telephone = null,
        string? description = null)
    {
        if (string.IsNullOrWhiteSpace(code))
            throw new ArgumentException("Warehouse code is required.");

        if (string.IsNullOrWhiteSpace(name))
            throw new ArgumentException("Warehouse name is required.");

        if (parentId.HasValue && parentId.Value == Guid.Empty)
            parentId = null;

        Id = Guid.NewGuid();
        Code = code.Trim();
        Name = name.Trim();
        ParentId = parentId;
        Address = address;
        Telephone = telephone;
        Description = description;
    }

    public void Rename(string name)
    {
        if (string.IsNullOrWhiteSpace(name))
            throw new ArgumentException("Warehouse name is required.");

        Name = name.Trim();
    }

    public void SetContact(string? address, string? telephone)
    {
        Address = address;
        Telephone = telephone;
    }

    public void SetDescription(string? description) => Description = description;
}
