namespace RGRE.ERP.Domain.Organization;

public interface ICompanyRepository
{
    Task<Company?> GetAsync(Guid id, CancellationToken cancellationToken = default);

    Task<Company?> GetByCodeAsync(string code, CancellationToken cancellationToken = default);

    Task<IReadOnlyList<Company>> ListAsync(CancellationToken cancellationToken = default);

    Task AddAsync(Company company, CancellationToken cancellationToken = default);
}

public interface IBranchRepository
{
    Task<Branch?> GetAsync(Guid id, CancellationToken cancellationToken = default);

    Task<IReadOnlyList<Branch>> ListAsync(CancellationToken cancellationToken = default);

    Task<IReadOnlyList<Branch>> ListByCompanyAsync(Guid companyId, CancellationToken cancellationToken = default);

    Task AddAsync(Branch branch, CancellationToken cancellationToken = default);
}
