using RGRE.ERP.Application.Abstractions;
using RGRE.ERP.Application.Inventory;
using RGRE.ERP.Application.Platform;
using RGRE.ERP.Domain.Accounting;
using RGRE.ERP.Domain.Inventory;
using RGRE.ERP.Domain.Inventory.Issues;

namespace RGRE.ERP.Application.Inventory.Issues;

public interface IIssueAppService
{
    Task<Guid> CreateAsync(CreateIssueDto input, CancellationToken cancellationToken = default);

    Task<IssueDto?> GetAsync(Guid issueId, CancellationToken cancellationToken = default);

    Task<IReadOnlyList<IssueDto>> ListAsync(CancellationToken cancellationToken = default);

    Task AddLineAsync(Guid issueId, AddIssueLineDto input, CancellationToken cancellationToken = default);

    Task RemoveLineAsync(Guid issueId, Guid lineId, CancellationToken cancellationToken = default);

    Task PostAsync(Guid issueId, CancellationToken cancellationToken = default);

    Task CancelAsync(Guid issueId, CancellationToken cancellationToken = default);
}

public sealed class IssueAppService : IIssueAppService
{
    private readonly IIssueRepository _issues;
    private readonly IGoodsRepository _goods;
    private readonly IStockLedgerService _ledger;
    private readonly IUnitOfWork _unitOfWork;
    private readonly IFiscalYearRepository _fiscalYears;
    private readonly ICurrentUserContext _userContext;

    public IssueAppService(
        IIssueRepository issues,
        IGoodsRepository goods,
        IStockLedgerService ledger,
        IUnitOfWork unitOfWork,
        IFiscalYearRepository fiscalYears,
        ICurrentUserContext userContext)
    {
        _issues = issues;
        _goods = goods;
        _ledger = ledger;
        _unitOfWork = unitOfWork;
        _fiscalYears = fiscalYears;
        _userContext = userContext;
    }

    public async Task<Guid> CreateAsync(CreateIssueDto input, CancellationToken cancellationToken = default)
    {
        var issue = new Issue(
            input.DocumentNumber,
            input.DocumentDate,
            input.WarehouseId,
            input.ReceiverPartnerId,
            input.CostCenterId,
            input.Description);

        foreach (var line in input.Lines)
        {
            await EnsureGoodsExistsAsync(line.GoodsId, cancellationToken);
            issue.AddLine(line.GoodsId, line.UnitId, line.Quantity, line.UnitPrice, line.Description);
        }

        await DocumentContextRules.StampCreationContextAsync(_fiscalYears, _userContext, issue, cancellationToken);

        await _issues.AddAsync(issue, cancellationToken);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        return issue.Id;
    }

    public async Task<IssueDto?> GetAsync(Guid issueId, CancellationToken cancellationToken = default)
    {
        var issue = await _issues.GetAsync(issueId, cancellationToken);

        return issue is null ? null : ToDto(issue);
    }

    public async Task<IReadOnlyList<IssueDto>> ListAsync(CancellationToken cancellationToken = default)
    {
        var list = await _issues.ListAsync(cancellationToken);

        return list.Select(ToDto).ToList();
    }

    public async Task AddLineAsync(Guid issueId, AddIssueLineDto input, CancellationToken cancellationToken = default)
    {
        var issue = await GetOrThrowAsync(issueId, cancellationToken);

        await EnsureGoodsExistsAsync(input.GoodsId, cancellationToken);

        issue.AddLine(input.GoodsId, input.UnitId, input.Quantity, input.UnitPrice, input.Description);

        await _unitOfWork.SaveChangesAsync(cancellationToken);
    }

    public async Task RemoveLineAsync(Guid issueId, Guid lineId, CancellationToken cancellationToken = default)
    {
        var issue = await GetOrThrowAsync(issueId, cancellationToken);

        issue.RemoveLine(lineId);

        await _unitOfWork.SaveChangesAsync(cancellationToken);
    }

    public async Task PostAsync(Guid issueId, CancellationToken cancellationToken = default)
    {
        var issue = await GetOrThrowAsync(issueId, cancellationToken);

        await DocumentContextRules.EnsureFiscalYearAcceptsAsync(_fiscalYears, _userContext, issue, cancellationToken);

        issue.Post();

        await _ledger.ApplyIssueAsync(issue, cancellationToken);

        await _unitOfWork.SaveChangesAsync(cancellationToken);
    }

    public async Task CancelAsync(Guid issueId, CancellationToken cancellationToken = default)
    {
        var issue = await GetOrThrowAsync(issueId, cancellationToken);

        issue.Cancel();

        await _unitOfWork.SaveChangesAsync(cancellationToken);
    }

    private async Task<Issue> GetOrThrowAsync(Guid id, CancellationToken cancellationToken)
    {
        return await _issues.GetAsync(id, cancellationToken)
            ?? throw new KeyNotFoundException($"Issue '{id}' was not found.");
    }

    private static IssueDto ToDto(Issue issue) => new()
    {
        Id = issue.Id,
        DocumentNumber = issue.DocumentNumber,
        DocumentDate = issue.DocumentDate,
        WarehouseId = issue.WarehouseId,
        ReceiverPartnerId = issue.ReceiverPartnerId,
        CostCenterId = issue.CostCenterId,
        Status = issue.Status.ToString(),
        Description = issue.Description,
        CompanyId = issue.CompanyId,
        BranchId = issue.BranchId,
        FiscalYearId = issue.FiscalYearId,
        Lines = issue.Lines.Select(l => new IssueLineDto
        {
            Id = l.Id,
            GoodsId = l.GoodsId,
            UnitId = l.UnitId,
            Quantity = l.Quantity,
            UnitPrice = l.UnitPrice,
            Amount = l.Amount,
            Description = l.Description,
        }).ToList(),
    }
    ;

    private async Task EnsureGoodsExistsAsync(Guid goodsId, CancellationToken cancellationToken)
    {
        _ = await _goods.GetAsync(goodsId, cancellationToken)
            ?? throw new KeyNotFoundException($"Goods '{goodsId}' was not found.");
    }
}

public sealed class CreateIssueDto
{
    public string DocumentNumber { get; set; } = null!;

    public DateTime DocumentDate { get; set; }

    public Guid WarehouseId { get; set; }

    public Guid? ReceiverPartnerId { get; set; }

    public Guid? CostCenterId { get; set; }

    public string? Description { get; set; }

    public List<AddIssueLineDto> Lines { get; set; } = new();
}

public sealed class AddIssueLineDto
{
    public Guid GoodsId { get; set; }

    public Guid UnitId { get; set; }

    public decimal Quantity { get; set; }

    public decimal UnitPrice { get; set; }

    public string? Description { get; set; }
}

public sealed class IssueLineDto
{
    public Guid Id { get; set; }

    public Guid GoodsId { get; set; }

    public Guid UnitId { get; set; }

    public decimal Quantity { get; set; }

    public decimal UnitPrice { get; set; }

    public decimal Amount { get; set; }

    public string? Description { get; set; }
}

public sealed class IssueDto
{
    public Guid Id { get; set; }

    public string DocumentNumber { get; set; } = null!;

    public DateTime DocumentDate { get; set; }

    public Guid WarehouseId { get; set; }

    public Guid? ReceiverPartnerId { get; set; }

    public Guid? CostCenterId { get; set; }

    public string Status { get; set; } = null!;

    public string? Description { get; set; }

    public Guid? CompanyId { get; set; }

    public Guid? BranchId { get; set; }

    public Guid? FiscalYearId { get; set; }

    public List<IssueLineDto> Lines { get; set; } = new();
}
