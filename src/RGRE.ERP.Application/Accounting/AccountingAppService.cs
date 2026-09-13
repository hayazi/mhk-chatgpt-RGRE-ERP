using RGRE.ERP.Application.Abstractions;
using RGRE.ERP.Application.Platform;
using RGRE.ERP.Domain.Accounting;

namespace RGRE.ERP.Application.Accounting;

// ---------- Chart of accounts ----------

public sealed class CreateLedgerAccountDto
{
    public string Code { get; set; } = null!;
    public string Name { get; set; } = null!;

    /// <summary>1 = group (kol), 2 = controlling (moin), 3+ = subsidiary (tafzili).</summary>
    public int Level { get; set; } = 1;

    public Guid? ParentId { get; set; }
    public bool IsProfitAndLoss { get; set; }
    public bool IsDebitNature { get; set; } = true;
    public bool AllowPosting { get; set; } = true;
}

public sealed class LedgerAccountDto
{
    public Guid Id { get; set; }
    public string Code { get; set; } = null!;
    public string Name { get; set; } = null!;
    public int Level { get; set; }
    public Guid? ParentId { get; set; }
    public bool AllowPosting { get; set; }
    public bool Enabled { get; set; }
}

public interface ILedgerAccountAppService
{
    Task<Guid> CreateAsync(CreateLedgerAccountDto input, CancellationToken cancellationToken = default);

    Task<IReadOnlyList<LedgerAccountDto>> ListAsync(CancellationToken cancellationToken = default);
}

public sealed class LedgerAccountAppService : ILedgerAccountAppService
{
    private readonly ILedgerAccountRepository _accounts;
    private readonly IUnitOfWork _unitOfWork;

    public LedgerAccountAppService(ILedgerAccountRepository accounts, IUnitOfWork unitOfWork)
    {
        _accounts = accounts;
        _unitOfWork = unitOfWork;
    }

    public async Task<Guid> CreateAsync(CreateLedgerAccountDto input, CancellationToken cancellationToken = default)
    {
        var account = new LedgerAccount(
            input.Code,
            input.Name,
            input.Level,
            input.ParentId,
            input.IsProfitAndLoss,
            input.IsDebitNature,
            input.AllowPosting);

        await _accounts.AddAsync(account, cancellationToken);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        return account.Id;
    }

    public async Task<IReadOnlyList<LedgerAccountDto>> ListAsync(CancellationToken cancellationToken = default)
    {
        var list = await _accounts.ListAsync(cancellationToken);

        return list.Select(a => new LedgerAccountDto
        {
            Id = a.Id,
            Code = a.Code,
            Name = a.Name,
            Level = a.Level,
            ParentId = a.ParentId,
            AllowPosting = a.AllowPosting,
            Enabled = a.Enabled,
        }).ToList();
    }
}

// ---------- Vouchers ----------

public sealed class AddVoucherLineDto
{
    public Guid AccountId { get; set; }
    public decimal Debit { get; set; }
    public decimal Credit { get; set; }
    public string? Description { get; set; }
    public Guid? PartnerId { get; set; }
    public Guid? WarehouseId { get; set; }
}

public sealed class CreateVoucherDto
{
    public string Number { get; set; } = null!;
    public DateTime Date { get; set; }
    public Guid? CategoryId { get; set; }
    public string? Description { get; set; }
    public List<AddVoucherLineDto> Lines { get; set; } = new();
}

public sealed class VoucherDto
{
    public Guid Id { get; set; }
    public string Number { get; set; } = null!;
    public DateTime Date { get; set; }
    public string Status { get; set; } = null!;    public decimal TotalDebit { get; set; }

    public decimal TotalCredit { get; set; }

    public int LineCount { get; set; }

    public Guid? CompanyId { get; set; }

    public Guid? BranchId { get; set; }

    public Guid? FiscalYearId { get; set; }
}

public interface IVoucherAppService
{
    Task<Guid> CreateAsync(CreateVoucherDto input, CancellationToken cancellationToken = default);

    Task AddLineAsync(Guid voucherId, AddVoucherLineDto input, CancellationToken cancellationToken = default);

    Task PostAsync(Guid voucherId, CancellationToken cancellationToken = default);

    Task CancelAsync(Guid voucherId, CancellationToken cancellationToken = default);

    Task<IReadOnlyList<VoucherDto>> ListAsync(CancellationToken cancellationToken = default);
}

public sealed class VoucherAppService : IVoucherAppService
{
    private readonly IVoucherRepository _vouchers;
    private readonly ILedgerAccountRepository _accounts;
    private readonly IUnitOfWork _unitOfWork;
    private readonly IFiscalYearRepository _fiscalYears;
    private readonly ICurrentUserContext _userContext;

    public VoucherAppService(
        IVoucherRepository vouchers,
        ILedgerAccountRepository accounts,
        IUnitOfWork unitOfWork,
        IFiscalYearRepository fiscalYears,
        ICurrentUserContext userContext)
    {
        _vouchers = vouchers;
        _accounts = accounts;
        _unitOfWork = unitOfWork;
        _fiscalYears = fiscalYears;
        _userContext = userContext;
    }

    public async Task<Guid> CreateAsync(CreateVoucherDto input, CancellationToken cancellationToken = default)
    {
        var voucher = Voucher.Create(input.Number, input.Date, input.CategoryId, input.Description);

        foreach (var line in input.Lines)
        {
            await EnsureAccountExistsAsync(line.AccountId, cancellationToken);
            voucher.AddLine(line.AccountId, line.Debit, line.Credit, line.Description, line.PartnerId, line.WarehouseId);
        }

        await DocumentContextRules.StampCreationContextAsync(_fiscalYears, _userContext, voucher, cancellationToken);

        await _vouchers.AddAsync(voucher, cancellationToken);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        return voucher.Id;
    }

    public async Task AddLineAsync(Guid voucherId, AddVoucherLineDto input, CancellationToken cancellationToken = default)
    {
        var voucher = await GetOrThrowAsync(voucherId, cancellationToken);

        await EnsureAccountExistsAsync(input.AccountId, cancellationToken);

        voucher.AddLine(input.AccountId, input.Debit, input.Credit, input.Description, input.PartnerId, input.WarehouseId);

        await _unitOfWork.SaveChangesAsync(cancellationToken);
    }

    public async Task PostAsync(Guid voucherId, CancellationToken cancellationToken = default)
    {
        var voucher = await GetOrThrowAsync(voucherId, cancellationToken);

        await DocumentContextRules.EnsureFiscalYearAcceptsAsync(_fiscalYears, _userContext, voucher, cancellationToken);

        voucher.Post();

        await _unitOfWork.SaveChangesAsync(cancellationToken);
    }

    public async Task CancelAsync(Guid voucherId, CancellationToken cancellationToken = default)
    {
        var voucher = await GetOrThrowAsync(voucherId, cancellationToken);

        voucher.Cancel();

        await _unitOfWork.SaveChangesAsync(cancellationToken);
    }

    public async Task<IReadOnlyList<VoucherDto>> ListAsync(CancellationToken cancellationToken = default)
    {
        var list = await _vouchers.ListAsync(cancellationToken);

        return list.Select(v => new VoucherDto
        {
            Id = v.Id,
            Number = v.Number,
            Date = v.Date,
            Status = v.Status.ToString(),
            TotalDebit = v.GetTotalDebit(),
            TotalCredit = v.GetTotalCredit(),
            LineCount = v.Lines.Count,
            CompanyId = v.CompanyId,
            BranchId = v.BranchId,
            FiscalYearId = v.FiscalYearId,
        }).ToList();
    }

    private async Task<Voucher> GetOrThrowAsync(Guid id, CancellationToken cancellationToken)
        => await _vouchers.GetAsync(id, cancellationToken)
            ?? throw new KeyNotFoundException($"Voucher '{id}' was not found.");

    private async Task EnsureAccountExistsAsync(Guid accountId, CancellationToken cancellationToken)
    {
        _ = await _accounts.GetAsync(accountId, cancellationToken)
            ?? throw new KeyNotFoundException($"Ledger account '{accountId}' was not found.");
    }
}
