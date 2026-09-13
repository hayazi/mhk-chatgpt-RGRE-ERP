using RGRE.ERP.Domain.Common;

namespace RGRE.ERP.Domain.Security;

/// <summary>
/// A named bundle of permissions. Replaces the legacy <c>Permissions</c>
/// action matrix (one row per user/form/action) with coarse role claims.
/// </summary>
public sealed class Role : AuditableEntity
{
    private readonly List<RolePermission> _permissions = new();

    public Guid Id { get; private set; }

    /// <summary>Unique role name, e.g. "Admin", "Accountant", "Warehouse".</summary>
    public string Name { get; private set; } = null!;

    public string? Description { get; private set; }

    public bool IsSystemRole { get; private set; }

    public IReadOnlyCollection<RolePermission> Permissions => _permissions.AsReadOnly();

    private Role()
    {
    }

    public Role(string name, string? description = null, bool isSystemRole = false)
    {
        if (string.IsNullOrWhiteSpace(name))
            throw new ArgumentException("Role name is required.");

        Id = Guid.NewGuid();
        Name = name.Trim();
        Description = description;
        IsSystemRole = isSystemRole;
    }

    public void Rename(string name)
    {
        if (string.IsNullOrWhiteSpace(name))
            throw new ArgumentException("Role name is required.");

        Name = name.Trim();
    }

    public void SetDescription(string? description) => Description = description;

    public void SetPermissions(IEnumerable<string> permissions)
    {
        ArgumentNullException.ThrowIfNull(permissions);

        _permissions.Clear();
        foreach (var permission in permissions)
        {
            if (!string.IsNullOrWhiteSpace(permission))
                _permissions.Add(new RolePermission(Id, permission.Trim()));
        }
    }
}

/// <summary>One permission granted by the role, e.g. "Inventory.Post".</summary>
public sealed class RolePermission
{
    public Guid RoleId { get; private set; }

    /// <summary>Stable permission string, e.g. "Goods", "Receipts.Post".</summary>
    public string Permission { get; private set; } = null!;

    private RolePermission()
    {
    }

    internal RolePermission(Guid roleId, string permission)
    {
        RoleId = roleId;
        Permission = permission;
    }
}

/// <summary>
/// Canonical permission strings used by the first milestone. Kept here so the
/// seeder, tests and UI agree on the vocabulary.
/// </summary>
public static class AppPermissions
{
    public const string Goods = "Goods";
    public const string Warehouses = "Warehouses";
    public const string Units = "Units";
    public const string Receipts = "Receipts";
    public const string ReceiptsPost = "Receipts.Post";
    public const string Issues = "Issues";
    public const string IssuesPost = "Issues.Post";
    public const string Transfers = "Transfers";
    public const string TransfersPost = "Transfers.Post";
    public const string Partners = "Partners";
    public const string Accounts = "Accounts";
    public const string Vouchers = "Vouchers";
    public const string VouchersPost = "Vouchers.Post";
    public const string Platform = "Platform";

    public static readonly IReadOnlyList<string> All =
    [
        Goods, Warehouses, Units, Receipts, ReceiptsPost,
        Issues, IssuesPost, Transfers, TransfersPost,
        Partners, Accounts, Vouchers, VouchersPost, Platform,
    ];
}
