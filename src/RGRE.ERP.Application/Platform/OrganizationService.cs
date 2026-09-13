using RGRE.ERP.Application.Abstractions;
using RGRE.ERP.Domain;
using RGRE.ERP.Domain.Accounting;
using RGRE.ERP.Domain.Common;
using RGRE.ERP.Domain.Organization;

namespace RGRE.ERP.Application.Platform;

// ---------- DTOs ----------

public sealed class CompanyDto
{
    public Guid Id { get; set; }

    public string Code { get; set; } = null!;

    public string Name { get; set; } = null!;

    public string? Name2 { get; set; }

    public string? EconomicCode { get; set; }

    public string? NationalId { get; set; }

    public bool Enabled { get; set; }
}

public sealed class CreateCompanyDto
{
    public string Code { get; set; } = null!;

    public string Name { get; set; } = null!;

    public string? Name2 { get; set; }

    public string? EconomicCode { get; set; }

    public string? NationalId { get; set; }
}

public sealed class BranchDto
{
    public Guid Id { get; set; }

    public Guid CompanyId { get; set; }

    public string Code { get; set; } = null!;

    public string Name { get; set; } = null!;

    public string? Address { get; set; }

    public string? Telephone { get; set; }

    public bool Enabled { get; set; }
}

public sealed class CreateBranchDto
{
    public Guid CompanyId { get; set; }

    public string Code { get; set; } = null!;

    public string Name { get; set; } = null!;

    public string? Address { get; set; }

    public string? Telephone { get; set; }
}

public sealed class FiscalYearDto
{
    public Guid Id { get; set; }

    public Guid CompanyId { get; set; }

    public string Name { get; set; } = null!;

    public DateTime StartDate { get; set; }

    public DateTime EndDate { get; set; }

    public bool IsClosed { get; set; }

    /// <summary>Jalali display of the start date, e.g. 1405/01/01.</summary>
    public string StartDateJalali { get; set; } = null!;

    /// <summary>Jalali display of the end date.</summary>
    public string EndDateJalali { get; set; } = null!;
}

public sealed class CreateFiscalYearDto
{
    public Guid CompanyId { get; set; }

    public string Name { get; set; } = null!;

    public DateTime StartDate { get; set; }

    public DateTime EndDate { get; set; }
}

// ---------- services ----------

public interface IOrganizationService
{
    Task<Guid> CreateCompanyAsync(CreateCompanyDto input, CancellationToken cancellationToken = default);

    Task<IReadOnlyList<CompanyDto>> ListCompaniesAsync(CancellationToken cancellationToken = default);

    Task SetCompanyEnabledAsync(Guid id, bool enabled, CancellationToken cancellationToken = default);

    Task<Guid> CreateBranchAsync(CreateBranchDto input, CancellationToken cancellationToken = default);

    Task<IReadOnlyList<BranchDto>> ListBranchesAsync(CancellationToken cancellationToken = default);
}

public sealed class OrganizationService : IOrganizationService
{
    private readonly ICompanyRepository _companies;
    private readonly IBranchRepository _branches;
    private readonly IUnitOfWork _unitOfWork;

    public OrganizationService(
        ICompanyRepository companies,
        IBranchRepository branches,
        IUnitOfWork unitOfWork)
    {
        _companies = companies;
        _branches = branches;
        _unitOfWork = unitOfWork;
    }

    public async Task<Guid> CreateCompanyAsync(
        CreateCompanyDto input,
        CancellationToken cancellationToken = default)
    {
        var existing = await _companies.GetByCodeAsync(input.Code, cancellationToken);

        if (existing is not null)
            throw new InvalidOperationException($"Company code '{input.Code}' already exists.");

        var company = new Company(
            input.Code,
            input.Name,
            input.Name2,
            input.EconomicCode,
            input.NationalId);

        await _companies.AddAsync(company, cancellationToken);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        return company.Id;
    }

    public Task<IReadOnlyList<CompanyDto>> ListCompaniesAsync(CancellationToken cancellationToken = default)
        => _companies.ListAsync(cancellationToken).ContinueWith<IReadOnlyList<CompanyDto>>(
            t => t.Result.Select(c => new CompanyDto
            {
                Id = c.Id,
                Code = c.Code,
                Name = c.Name,
                Name2 = c.Name2,
                EconomicCode = c.EconomicCode,
                NationalId = c.NationalId,
                Enabled = c.Enabled,
            }).ToList(),
            cancellationToken,
            TaskContinuationOptions.ExecuteSynchronously,
            TaskScheduler.Default);

    public async Task SetCompanyEnabledAsync(
        Guid id,
        bool enabled,
        CancellationToken cancellationToken = default)
    {
        var company = await _companies.GetAsync(id, cancellationToken)
            ?? throw new KeyNotFoundException($"Company '{id}' was not found.");

        if (enabled)
            company.Enable();
        else
            company.Disable();

        await _unitOfWork.SaveChangesAsync(cancellationToken);
    }

    public async Task<Guid> CreateBranchAsync(
        CreateBranchDto input,
        CancellationToken cancellationToken = default)
    {
        _ = await _companies.GetAsync(input.CompanyId, cancellationToken)
            ?? throw new KeyNotFoundException($"Company '{input.CompanyId}' was not found.");

        var branch = new Branch(
            input.CompanyId,
            input.Code,
            input.Name,
            input.Address,
            input.Telephone);

        await _branches.AddAsync(branch, cancellationToken);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        return branch.Id;
    }

    public Task<IReadOnlyList<BranchDto>> ListBranchesAsync(CancellationToken cancellationToken = default)
        => _branches.ListAsync(cancellationToken).ContinueWith<IReadOnlyList<BranchDto>>(
            t => t.Result.Select(b => new BranchDto
            {
                Id = b.Id,
                CompanyId = b.CompanyId,
                Code = b.Code,
                Name = b.Name,
                Address = b.Address,
                Telephone = b.Telephone,
                Enabled = b.Enabled,
            }).ToList(),
            cancellationToken,
            TaskContinuationOptions.ExecuteSynchronously,
            TaskScheduler.Default);
}

public interface IFiscalYearService
{
    Task<Guid> CreateAsync(CreateFiscalYearDto input, CancellationToken cancellationToken = default);

    Task<IReadOnlyList<FiscalYearDto>> ListAsync(CancellationToken cancellationToken = default);

    Task<FiscalYearDto?> GetOpenAsync(CancellationToken cancellationToken = default);

    Task CloseAsync(Guid id, CancellationToken cancellationToken = default);

    Task ReopenAsync(Guid id, CancellationToken cancellationToken = default);

    /// <summary>Returns the open fiscal year or throws; used by document services.</summary>
    Task<Domain.Accounting.FiscalYear> GetOpenOrThrowAsync(CancellationToken cancellationToken = default);
}

public sealed class FiscalYearService : IFiscalYearService
{
    private readonly IFiscalYearRepository _fiscalYears;
    private readonly IUnitOfWork _unitOfWork;

    public FiscalYearService(IFiscalYearRepository fiscalYears, IUnitOfWork unitOfWork)
    {
        _fiscalYears = fiscalYears;
        _unitOfWork = unitOfWork;
    }

    public async Task<Guid> CreateAsync(CreateFiscalYearDto input, CancellationToken cancellationToken = default)
    {
        var fiscalYear = new Domain.Accounting.FiscalYear(
            input.CompanyId,
            input.Name,
            input.StartDate,
            input.EndDate);

        await _fiscalYears.AddAsync(fiscalYear, cancellationToken);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        return fiscalYear.Id;
    }

    public Task<IReadOnlyList<FiscalYearDto>> ListAsync(CancellationToken cancellationToken = default)
        => _fiscalYears.ListAsync(cancellationToken).ContinueWith<IReadOnlyList<FiscalYearDto>>(
            t => t.Result.Select(ToDto).ToList(),
            cancellationToken,
            TaskContinuationOptions.ExecuteSynchronously,
            TaskScheduler.Default);

    public async Task<FiscalYearDto?> GetOpenAsync(CancellationToken cancellationToken = default)
    {
        var fy = await _fiscalYears.GetOpenAsync(cancellationToken);
        return fy is null ? null : ToDto(fy);
    }

    public async Task CloseAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var fy = await GetOrThrowAsync(id, cancellationToken);

        fy.Close();
        await _unitOfWork.SaveChangesAsync(cancellationToken);
    }

    public async Task ReopenAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var fy = await GetOrThrowAsync(id, cancellationToken);

        fy.Reopen();
        await _unitOfWork.SaveChangesAsync(cancellationToken);
    }

    public async Task<Domain.Accounting.FiscalYear> GetOpenOrThrowAsync(
        CancellationToken cancellationToken = default)
        => await _fiscalYears.GetOpenAsync(cancellationToken)
            ?? throw new InvalidOperationException(
                "No open fiscal year exists. Create and open a fiscal year first.");

    private static FiscalYearDto ToDto(Domain.Accounting.FiscalYear fy) => new()
    {
        Id = fy.Id,
        CompanyId = fy.CompanyId,
        Name = fy.Name,
        StartDate = fy.StartDate,
        EndDate = fy.EndDate,
        IsClosed = fy.IsClosed,
        StartDateJalali = JalaliDate.Format(fy.StartDate),
        EndDateJalali = JalaliDate.Format(fy.EndDate),
    };

    private async Task<Domain.Accounting.FiscalYear> GetOrThrowAsync(
        Guid id,
        CancellationToken cancellationToken)
        => await _fiscalYears.GetAsync(id, cancellationToken)
            ?? throw new KeyNotFoundException($"Fiscal year '{id}' was not found.");
}
