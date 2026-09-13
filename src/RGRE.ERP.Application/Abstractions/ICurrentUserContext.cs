namespace RGRE.ERP.Application.Abstractions;

/// <summary>
/// The logged-in user and the active working context (company / branch /
/// fiscal year). Implemented in the Web layer over the auth cookie;
/// document services stamp these dimensions onto aggregates.
/// </summary>
public interface ICurrentUserContext
{
    bool IsAuthenticated { get; }

    Guid? UserId { get; }

    string? UserName { get; }

    string? DisplayName { get; }

    /// <summary>Active company (legacy <c>CoID</c>).</summary>
    Guid? CompanyId { get; }

    /// <summary>Active branch (legacy <c>BrID</c>).</summary>
    Guid? BranchId { get; }

    /// <summary>Active fiscal year (legacy <c>FyID</c>).</summary>
    Guid? FiscalYearId { get; }

    /// <summary>True when the user holds the given permission.</summary>
    bool HasPermission(string permission);
}

/// <summary>
/// Anonymous context used by background jobs and tests without a signed-in user.
/// </summary>
public sealed class AnonymousUserContext : ICurrentUserContext
{
    public static readonly AnonymousUserContext Instance = new();

    public bool IsAuthenticated => false;

    public Guid? UserId => null;

    public string? UserName => null;

    public string? DisplayName => null;

    public Guid? CompanyId => null;

    public Guid? BranchId => null;

    public Guid? FiscalYearId => null;

    public bool HasPermission(string permission) => false;
}

/// <summary>Password hashing abstraction (PBKDF2 in Infrastructure).</summary>
public interface IPasswordHasher
{
    /// <summary>Hashes a password into the storage format.</summary>
    string Hash(string password);

    /// <summary>Verifies a password against a stored hash.</summary>
    bool Verify(string password, string storedHash);
}
