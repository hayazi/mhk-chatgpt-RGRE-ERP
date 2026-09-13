using RGRE.ERP.Domain.Common;

namespace RGRE.ERP.Domain.Security;

/// <summary>
/// An application user. Replaces the legacy <c>Users</c> side of the
/// <c>Permissions*</c> matrix (the permission side lives in <see cref="Role"/>).
/// </summary>
public sealed class User : AuditableEntity
{
    private readonly List<UserRole> _roles = new();

    public Guid Id { get; private set; }

    /// <summary>Login name, unique, case-insensitive.</summary>
    public string Username { get; private set; } = null!;

    /// <summary>Legacy <c>Name</c> / <c>Family</c> collapse into one display name.</summary>
    public string DisplayName { get; private set; } = null!;

    /// <summary>
    /// PBKDF2 hash in "iterations.saltB64.hashB64" form. Never contains the password.
    /// </summary>
    public string PasswordHash { get; private set; } = null!;

    /// <summary>Legacy <c>Enabled</c> flag on the Users table.</summary>
    public bool Enabled { get; private set; }

    /// <summary>Count of consecutive failed logins (cleared on success).</summary>
    public int FailedLoginCount { get; private set; }

    /// <summary>Utc time until which the account is locked out (null = not locked).</summary>
    public DateTime? LockedOutUntilUtc { get; private set; }

    public Guid? LastCompanyId { get; private set; }

    public Guid? LastBranchId { get; private set; }

    public Guid? LastFiscalYearId { get; private set; }

    public IReadOnlyCollection<UserRole> Roles => _roles.AsReadOnly();

    private User()
    {
    }

    public User(string username, string displayName, string passwordHash)
    {
        if (string.IsNullOrWhiteSpace(username))
            throw new ArgumentException("Username is required.");

        if (string.IsNullOrWhiteSpace(displayName))
            throw new ArgumentException("Display name is required.");

        if (string.IsNullOrWhiteSpace(passwordHash))
            throw new ArgumentException("Password hash is required.");

        Id = Guid.NewGuid();
        Username = username.Trim().ToLowerInvariant();
        DisplayName = displayName.Trim();
        PasswordHash = passwordHash;
        Enabled = true;
    }

    public void SetPassword(string passwordHash)
    {
        if (string.IsNullOrWhiteSpace(passwordHash))
            throw new ArgumentException("Password hash is required.");

        PasswordHash = passwordHash;
    }

    public void Rename(string displayName)
    {
        if (string.IsNullOrWhiteSpace(displayName))
            throw new ArgumentException("Display name is required.");

        DisplayName = displayName.Trim();
    }

    public void Disable() => Enabled = false;

    public void Enable() => Enabled = true;

    public bool CanLogin(DateTime utcNow)
        => Enabled && (LockedOutUntilUtc is null || LockedOutUntilUtc.Value <= utcNow);

    public void RegisterFailedLogin(DateTime utcNow, int lockoutThreshold, int lockoutMinutes)
    {
        FailedLoginCount++;

        if (lockoutThreshold > 0 && FailedLoginCount >= lockoutThreshold)
            LockedOutUntilUtc = utcNow.AddMinutes(lockoutMinutes);
    }

    public void RegisterSuccessfulLogin()
    {
        FailedLoginCount = 0;
        LockedOutUntilUtc = null;
    }

    public void AddRole(Guid roleId)
    {
        if (roleId == Guid.Empty)
            throw new ArgumentException("Role id is required.");

        if (_roles.Any(r => r.RoleId == roleId))
            return;

        _roles.Add(new UserRole(Id, roleId));
    }

    public void RemoveRole(Guid roleId)
    {
        var link = _roles.FirstOrDefault(r => r.RoleId == roleId);

        if (link is not null)
            _roles.Remove(link);
    }

    public void SetLastContext(Guid? companyId, Guid? branchId, Guid? fiscalYearId)
    {
        LastCompanyId = companyId;
        LastBranchId = branchId;
        LastFiscalYearId = fiscalYearId;
    }
}

/// <summary>Assignment of a role to a user (legacy Users ↔ Permissions link).</summary>
public sealed class UserRole
{
    public Guid UserId { get; private set; }

    public Guid RoleId { get; private set; }

    private UserRole()
    {
    }

    internal UserRole(Guid userId, Guid roleId)
    {
        UserId = userId;
        RoleId = roleId;
    }
}
