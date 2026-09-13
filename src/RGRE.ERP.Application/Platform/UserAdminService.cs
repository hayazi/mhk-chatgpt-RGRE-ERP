using RGRE.ERP.Application.Abstractions;
using RGRE.ERP.Domain.Security;

namespace RGRE.ERP.Application.Platform;

// ---------- DTOs ----------

public sealed class UserDto
{
    public Guid Id { get; set; }

    public string Username { get; set; } = null!;

    public string DisplayName { get; set; } = null!;

    public bool Enabled { get; set; }

    public bool IsLockedOut { get; set; }

    public List<Guid> RoleIds { get; set; } = new();
}

public sealed class CreateUserDto
{
    public string Username { get; set; } = null!;

    public string DisplayName { get; set; } = null!;

    public string Password { get; set; } = null!;

    public List<Guid> RoleIds { get; set; } = new();
}

public sealed class RoleDto
{
    public Guid Id { get; set; }

    public string Name { get; set; } = null!;

    public string? Description { get; set; }

    public bool IsSystemRole { get; set; }

    public List<string> Permissions { get; set; } = new();
}

public sealed class CreateRoleDto
{
    public string Name { get; set; } = null!;

    public string? Description { get; set; }

    public List<string> Permissions { get; set; } = new();
}

public sealed class SetPasswordDto
{
    public string Password { get; set; } = null!;
}

public sealed class SetRolesDto
{
    public List<Guid> RoleIds { get; set; } = new();
}

public sealed class SetPermissionsDto
{
    public List<string> Permissions { get; set; } = new();
}

// ---------- services ----------

public interface IUserAdminService
{
    Task<Guid> CreateAsync(CreateUserDto input, CancellationToken cancellationToken = default);

    Task<IReadOnlyList<UserDto>> ListAsync(CancellationToken cancellationToken = default);

    Task SetPasswordAsync(Guid userId, string password, CancellationToken cancellationToken = default);

    Task SetRolesAsync(Guid userId, IReadOnlyCollection<Guid> roleIds, CancellationToken cancellationToken = default);

    Task SetEnabledAsync(Guid userId, bool enabled, CancellationToken cancellationToken = default);

    Task UnlockAsync(Guid userId, CancellationToken cancellationToken = default);
}

public sealed class UserAdminService : IUserAdminService
{
    public const int LockoutThreshold = 5;
    public const int LockoutMinutes = 15;

    private readonly IUserRepository _users;
    private readonly IRoleRepository _roles;
    private readonly IPasswordHasher _passwordHasher;
    private readonly IUnitOfWork _unitOfWork;
    private readonly IClock _clock;

    public UserAdminService(
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

    public async Task<Guid> CreateAsync(CreateUserDto input, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(input.Password))
            throw new ArgumentException("Password is required.");

        var existing = await _users.GetByUsernameAsync(input.Username, cancellationToken);

        if (existing is not null)
            throw new InvalidOperationException($"Username '{input.Username}' is already taken.");

        var user = new User(
            input.Username,
            input.DisplayName,
            _passwordHasher.Hash(input.Password));

        foreach (var roleId in input.RoleIds)
        {
            _ = await _roles.GetAsync(roleId, cancellationToken)
                ?? throw new KeyNotFoundException($"Role '{roleId}' was not found.");

            user.AddRole(roleId);
        }

        await _users.AddAsync(user, cancellationToken);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        return user.Id;
    }

    public Task<IReadOnlyList<UserDto>> ListAsync(CancellationToken cancellationToken = default)
    {
        // Users are a tiny table; listing with roles via the repository keeps
        // the app service free of query concerns.
        return _users.ListAsync(cancellationToken).ContinueWith<IReadOnlyList<UserDto>>(
            t => t.Result.Select(ToDto).ToList(),
            cancellationToken,
            TaskContinuationOptions.ExecuteSynchronously,
            TaskScheduler.Default);
    }

    public async Task SetPasswordAsync(
        Guid userId,
        string password,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(password))
            throw new ArgumentException("Password is required.");

        var user = await GetOrThrowAsync(userId, cancellationToken);

        user.SetPassword(_passwordHasher.Hash(password));
        await _unitOfWork.SaveChangesAsync(cancellationToken);
    }

    public async Task SetRolesAsync(
        Guid userId,
        IReadOnlyCollection<Guid> roleIds,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(roleIds);

        var user = await GetOrThrowAsync(userId, cancellationToken);

        foreach (var roleId in user.Roles.Select(r => r.RoleId).ToList())
        {
            if (!roleIds.Contains(roleId))
                user.RemoveRole(roleId);
        }

        foreach (var roleId in roleIds)
        {
            if (user.Roles.All(r => r.RoleId != roleId))
            {
                _ = await _roles.GetAsync(roleId, cancellationToken)
                    ?? throw new KeyNotFoundException($"Role '{roleId}' was not found.");

                user.AddRole(roleId);
            }
        }

        await _unitOfWork.SaveChangesAsync(cancellationToken);
    }

    public async Task SetEnabledAsync(
        Guid userId,
        bool enabled,
        CancellationToken cancellationToken = default)
    {
        var user = await GetOrThrowAsync(userId, cancellationToken);

        if (enabled)
            user.Enable();
        else
            user.Disable();

        await _unitOfWork.SaveChangesAsync(cancellationToken);
    }

    public async Task UnlockAsync(Guid userId, CancellationToken cancellationToken = default)
    {
        var user = await GetOrThrowAsync(userId, cancellationToken);

        user.RegisterSuccessfulLogin();
        await _unitOfWork.SaveChangesAsync(cancellationToken);
    }

    private async Task<User> GetOrThrowAsync(Guid id, CancellationToken cancellationToken)
        => await _users.GetAsync(id, cancellationToken)
            ?? throw new KeyNotFoundException($"User '{id}' was not found.");

    private UserDto ToDto(User user) => new()
    {
        Id = user.Id,
        Username = user.Username,
        DisplayName = user.DisplayName,
        Enabled = user.Enabled,
        IsLockedOut = user.LockedOutUntilUtc is not null
            && user.LockedOutUntilUtc.Value > _clock.UtcNow,
        RoleIds = user.Roles.Select(r => r.RoleId).ToList(),
    };
}

public interface IRoleAdminService
{
    Task<Guid> CreateAsync(CreateRoleDto input, CancellationToken cancellationToken = default);

    Task<IReadOnlyList<RoleDto>> ListAsync(CancellationToken cancellationToken = default);

    Task SetPermissionsAsync(
        Guid roleId,
        IReadOnlyCollection<string> permissions,
        CancellationToken cancellationToken = default);
}

public sealed class RoleAdminService : IRoleAdminService
{
    private readonly IRoleRepository _roles;
    private readonly IUnitOfWork _unitOfWork;

    public RoleAdminService(IRoleRepository roles, IUnitOfWork unitOfWork)
    {
        _roles = roles;
        _unitOfWork = unitOfWork;
    }

    public async Task<Guid> CreateAsync(CreateRoleDto input, CancellationToken cancellationToken = default)
    {
        var existing = await _roles.GetByNameAsync(input.Name, cancellationToken);

        if (existing is not null)
            throw new InvalidOperationException($"Role '{input.Name}' already exists.");

        var role = new Role(input.Name, input.Description);

        role.SetPermissions(input.Permissions);
        await _roles.AddAsync(role, cancellationToken);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        return role.Id;
    }

    public Task<IReadOnlyList<RoleDto>> ListAsync(CancellationToken cancellationToken = default)
        => _roles.ListAsync(cancellationToken).ContinueWith<IReadOnlyList<RoleDto>>(
            t => t.Result.Select(ToDto).ToList(),
            cancellationToken,
            TaskContinuationOptions.ExecuteSynchronously,
            TaskScheduler.Default);

    public async Task SetPermissionsAsync(
        Guid roleId,
        IReadOnlyCollection<string> permissions,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(permissions);

        var role = await _roles.GetAsync(roleId, cancellationToken)
            ?? throw new KeyNotFoundException($"Role '{roleId}' was not found.");

        role.SetPermissions(permissions);
        await _unitOfWork.SaveChangesAsync(cancellationToken);
    }

    private static RoleDto ToDto(Role role) => new()
    {
        Id = role.Id,
        Name = role.Name,
        Description = role.Description,
        IsSystemRole = role.IsSystemRole,
        Permissions = role.Permissions.Select(p => p.Permission).ToList(),
    };
}
