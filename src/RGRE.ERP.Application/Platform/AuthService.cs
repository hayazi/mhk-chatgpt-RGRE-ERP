using RGRE.ERP.Application.Abstractions;
using RGRE.ERP.Domain.Accounting;
using RGRE.ERP.Domain.Security;

namespace RGRE.ERP.Application.Platform;

/// <summary>
/// Sign-in use case: verifies credentials, applies lockout, and hands back
/// the user + permission set for the caller to establish a session.
/// </summary>
public interface IAuthService
{
    Task<AuthResult> LoginAsync(
        string username,
        string password,
        CancellationToken cancellationToken = default);
}

public sealed record AuthResult(
    bool Success,
    string? Error,
    Guid? UserId,
    string? DisplayName,
    IReadOnlyList<string> Permissions,
    Guid? LastCompanyId,
    Guid? LastBranchId,
    Guid? LastFiscalYearId)
{
    public static AuthResult Fail(string error) =>
        new(false, error, null, null, Array.Empty<string>(), null, null, null);
}

public sealed class AuthService : IAuthService
{
    private readonly IUserRepository _users;
    private readonly IRoleRepository _roles;
    private readonly IPasswordHasher _passwordHasher;
    private readonly IUnitOfWork _unitOfWork;
    private readonly IClock _clock;

    public AuthService(
        IUserRepository users,
        IRoleRepository roles,
        IPasswordHasher passwordHasher,
        IUnitOfWork unitOfWork,
        IClock clock)
    {
        _users = users;
        _roles = roles;
        _passwordHasher = passwordHasher;
        _unitOfWork = unitOfWork;
        _clock = clock;
    }

    public async Task<AuthResult> LoginAsync(
        string username,
        string password,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(username) || string.IsNullOrWhiteSpace(password))
            return AuthResult.Fail("Username and password are required.");

        var user = await _users.GetByUsernameAsync(username, cancellationToken);

        if (user is null)
            return AuthResult.Fail("Invalid username or password.");

        if (!user.CanLogin(_clock.UtcNow))
            return AuthResult.Fail(
                "Account is locked due to repeated failed logins. Try again later or ask an admin to unlock it.");

        if (!_passwordHasher.Verify(password, user.PasswordHash))
        {
            user.RegisterFailedLogin(
                _clock.UtcNow,
                UserAdminService.LockoutThreshold,
                UserAdminService.LockoutMinutes);

            await _unitOfWork.SaveChangesAsync(cancellationToken);

            return AuthResult.Fail("Invalid username or password.");
        }

        user.RegisterSuccessfulLogin();
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        // UserRole links only carry role ids; permissions live on the Role.
        var permissions = new List<string>();

        foreach (var roleId in user.Roles.Select(r => r.RoleId))
        {
            var role = await _roles.GetAsync(roleId, cancellationToken);

            if (role is not null)
                permissions.AddRange(role.Permissions.Select(p => p.Permission));
        }

        return new AuthResult(
            Success: true,
            Error: null,
            UserId: user.Id,
            DisplayName: user.DisplayName,
            Permissions: permissions.Distinct().ToList(),
            LastCompanyId: user.LastCompanyId,
            LastBranchId: user.LastBranchId,
            LastFiscalYearId: user.LastFiscalYearId);
    }
}
