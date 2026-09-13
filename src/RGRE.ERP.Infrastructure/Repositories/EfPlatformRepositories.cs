using Microsoft.EntityFrameworkCore;
using RGRE.ERP.Domain.Accounting;
using RGRE.ERP.Domain.Organization;
using RGRE.ERP.Domain.Security;
using RGRE.ERP.Infrastructure.Persistence;

namespace RGRE.ERP.Infrastructure.Repositories;

public class UserRepository : IUserRepository
{
    private readonly ErpDbContext _db;

    public UserRepository(ErpDbContext db) => _db = db;

    public Task<User?> GetAsync(Guid id, CancellationToken cancellationToken = default)
        => _db.Users.Include(u => u.Roles)
            .FirstOrDefaultAsync(x => x.Id == id, cancellationToken);

    public Task<User?> GetByUsernameAsync(string username, CancellationToken cancellationToken = default)
        => _db.Users.Include(u => u.Roles)
            .FirstOrDefaultAsync(x => x.Username == username.Trim().ToLowerInvariant(), cancellationToken);

    public async Task<IReadOnlyList<User>> ListAsync(CancellationToken cancellationToken = default)
        => await _db.Users.AsNoTracking()
            .Include(u => u.Roles)
            .OrderBy(x => x.Username)
            .ToListAsync(cancellationToken);

    public async Task AddAsync(User user, CancellationToken cancellationToken = default)
        => await _db.Users.AddAsync(user, cancellationToken);
}

public class RoleRepository : IRoleRepository
{
    private readonly ErpDbContext _db;

    public RoleRepository(ErpDbContext db) => _db = db;

    public Task<Role?> GetAsync(Guid id, CancellationToken cancellationToken = default)
        => _db.Roles.Include(r => r.Permissions)
            .FirstOrDefaultAsync(x => x.Id == id, cancellationToken);

    public Task<Role?> GetByNameAsync(string name, CancellationToken cancellationToken = default)
        => _db.Roles.Include(r => r.Permissions)
            .FirstOrDefaultAsync(x => x.Name == name, cancellationToken);

    public async Task<IReadOnlyList<Role>> ListAsync(CancellationToken cancellationToken = default)
        => await _db.Roles.AsNoTracking()
            .Include(r => r.Permissions)
            .OrderBy(x => x.Name)
            .ToListAsync(cancellationToken);

    public async Task AddAsync(Role role, CancellationToken cancellationToken = default)
        => await _db.Roles.AddAsync(role, cancellationToken);
}

public class CompanyRepository : ICompanyRepository
{
    private readonly ErpDbContext _db;

    public CompanyRepository(ErpDbContext db) => _db = db;

    public Task<Company?> GetAsync(Guid id, CancellationToken cancellationToken = default)
        => _db.Companies.FirstOrDefaultAsync(x => x.Id == id, cancellationToken);

    public Task<Company?> GetByCodeAsync(string code, CancellationToken cancellationToken = default)
        => _db.Companies.FirstOrDefaultAsync(x => x.Code == code, cancellationToken);

    public async Task<IReadOnlyList<Company>> ListAsync(CancellationToken cancellationToken = default)
        => await _db.Companies.AsNoTracking()
            .OrderBy(x => x.Code)
            .ToListAsync(cancellationToken);

    public async Task AddAsync(Company company, CancellationToken cancellationToken = default)
        => await _db.Companies.AddAsync(company, cancellationToken);
}

public class BranchRepository : IBranchRepository
{
    private readonly ErpDbContext _db;

    public BranchRepository(ErpDbContext db) => _db = db;

    public Task<Branch?> GetAsync(Guid id, CancellationToken cancellationToken = default)
        => _db.Branches.FirstOrDefaultAsync(x => x.Id == id, cancellationToken);

    public async Task<IReadOnlyList<Branch>> ListAsync(CancellationToken cancellationToken = default)
        => await _db.Branches.AsNoTracking()
            .OrderBy(x => x.Code)
            .ToListAsync(cancellationToken);

    public async Task<IReadOnlyList<Branch>> ListByCompanyAsync(
        Guid companyId, CancellationToken cancellationToken = default)
        => await _db.Branches.AsNoTracking()
            .Where(x => x.CompanyId == companyId)
            .OrderBy(x => x.Code)
            .ToListAsync(cancellationToken);

    public async Task AddAsync(Branch branch, CancellationToken cancellationToken = default)
        => await _db.Branches.AddAsync(branch, cancellationToken);
}

public class FiscalYearRepository : IFiscalYearRepository
{
    private readonly ErpDbContext _db;

    public FiscalYearRepository(ErpDbContext db) => _db = db;

    public Task<FiscalYear?> GetAsync(Guid id, CancellationToken cancellationToken = default)
        => _db.FiscalYears.FirstOrDefaultAsync(x => x.Id == id, cancellationToken);

    public Task<FiscalYear?> GetOpenAsync(CancellationToken cancellationToken = default)
        => _db.FiscalYears.AsNoTracking()
            .FirstOrDefaultAsync(x => !x.IsClosed, cancellationToken);

    public async Task<IReadOnlyList<FiscalYear>> ListAsync(CancellationToken cancellationToken = default)
        => await _db.FiscalYears.AsNoTracking()
            .OrderByDescending(x => x.StartDate)
            .ToListAsync(cancellationToken);

    public async Task AddAsync(FiscalYear fiscalYear, CancellationToken cancellationToken = default)
        => await _db.FiscalYears.AddAsync(fiscalYear, cancellationToken);
}
