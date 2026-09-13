namespace RGRE.ERP.Domain.Security;

public interface IUserRepository
{
    Task<User?> GetAsync(Guid id, CancellationToken cancellationToken = default);

    Task<User?> GetByUsernameAsync(string username, CancellationToken cancellationToken = default);

    Task<IReadOnlyList<User>> ListAsync(CancellationToken cancellationToken = default);

    Task AddAsync(User user, CancellationToken cancellationToken = default);
}

public interface IRoleRepository
{
    Task<Role?> GetAsync(Guid id, CancellationToken cancellationToken = default);

    Task<Role?> GetByNameAsync(string name, CancellationToken cancellationToken = default);

    Task<IReadOnlyList<Role>> ListAsync(CancellationToken cancellationToken = default);

    Task AddAsync(Role role, CancellationToken cancellationToken = default);
}
