using RGRE.ERP.Application.Abstractions;
using RGRE.ERP.Application.Inventory;
using RGRE.ERP.Application.Platform;
using RGRE.ERP.Domain.Accounting;
using RGRE.ERP.Domain.Inventory;
using RGRE.ERP.Domain.Inventory.Transfers;

namespace RGRE.ERP.Application.Inventory.Transfers;

public interface IStockTransferAppService
{
    Task<Guid> CreateAsync(CreateStockTransferDto input, CancellationToken cancellationToken = default);

    Task<StockTransferDto?> GetAsync(Guid transferId, CancellationToken cancellationToken = default);

    Task<IReadOnlyList<StockTransferDto>> ListAsync(CancellationToken cancellationToken = default);

    Task AddLineAsync(Guid transferId, AddStockTransferLineDto input, CancellationToken cancellationToken = default);

    Task PostAsync(Guid transferId, CancellationToken cancellationToken = default);

    Task CancelAsync(Guid transferId, CancellationToken cancellationToken = default);
}

public sealed class StockTransferAppService : IStockTransferAppService
{
    private readonly IStockTransferRepository _transfers;
    private readonly IGoodsRepository _goods;
    private readonly IStockLedgerService _ledger;
    private readonly IUnitOfWork _unitOfWork;
    private readonly IFiscalYearRepository _fiscalYears;
    private readonly ICurrentUserContext _userContext;

    public StockTransferAppService(
        IStockTransferRepository transfers,
        IGoodsRepository goods,
        IStockLedgerService ledger,
        IUnitOfWork unitOfWork,
        IFiscalYearRepository fiscalYears,
        ICurrentUserContext userContext)
    {
        _transfers = transfers;
        _goods = goods;
        _ledger = ledger;
        _unitOfWork = unitOfWork;
        _fiscalYears = fiscalYears;
        _userContext = userContext;
    }

    public async Task<Guid> CreateAsync(CreateStockTransferDto input, CancellationToken cancellationToken = default)
    {
        var transfer = new StockTransfer(
            input.DocumentNumber,
            input.DocumentDate,
            input.FromWarehouseId,
            input.ToWarehouseId,
            input.Description);

        foreach (var line in input.Lines)
        {
            await EnsureGoodsExistsAsync(line.GoodsId, cancellationToken);
            transfer.AddLine(line.GoodsId, line.UnitId, line.Quantity, line.Description);
        }

        await DocumentContextRules.StampCreationContextAsync(_fiscalYears, _userContext, transfer, cancellationToken);

        await _transfers.AddAsync(transfer, cancellationToken);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        return transfer.Id;
    }

    public async Task<StockTransferDto?> GetAsync(Guid transferId, CancellationToken cancellationToken = default)
    {
        var transfer = await _transfers.GetAsync(transferId, cancellationToken);

        return transfer is null ? null : ToDto(transfer);
    }

    public async Task<IReadOnlyList<StockTransferDto>> ListAsync(CancellationToken cancellationToken = default)
    {
        var list = await _transfers.ListAsync(cancellationToken);

        return list.Select(ToDto).ToList();
    }

    public async Task AddLineAsync(Guid transferId, AddStockTransferLineDto input, CancellationToken cancellationToken = default)
    {
        var transfer = await GetOrThrowAsync(transferId, cancellationToken);

        await EnsureGoodsExistsAsync(input.GoodsId, cancellationToken);

        transfer.AddLine(input.GoodsId, input.UnitId, input.Quantity, input.Description);

        await _unitOfWork.SaveChangesAsync(cancellationToken);
    }

    public async Task PostAsync(Guid transferId, CancellationToken cancellationToken = default)
    {
        var transfer = await GetOrThrowAsync(transferId, cancellationToken);

        await DocumentContextRules.EnsureFiscalYearAcceptsAsync(_fiscalYears, _userContext, transfer, cancellationToken);

        transfer.Post();

        await _ledger.ApplyTransferAsync(transfer, cancellationToken);

        await _unitOfWork.SaveChangesAsync(cancellationToken);
    }

    public async Task CancelAsync(Guid transferId, CancellationToken cancellationToken = default)
    {
        var transfer = await GetOrThrowAsync(transferId, cancellationToken);

        transfer.Cancel();

        await _unitOfWork.SaveChangesAsync(cancellationToken);
    }

    private async Task<StockTransfer> GetOrThrowAsync(Guid id, CancellationToken cancellationToken)
    {
        return await _transfers.GetAsync(id, cancellationToken)
            ?? throw new KeyNotFoundException($"Transfer '{id}' was not found.");
    }

    private static StockTransferDto ToDto(StockTransfer transfer) => new()
    {
        Id = transfer.Id,
        DocumentNumber = transfer.DocumentNumber,
        DocumentDate = transfer.DocumentDate,
        FromWarehouseId = transfer.FromWarehouseId,
        ToWarehouseId = transfer.ToWarehouseId,
        Status = transfer.Status.ToString(),
        Description = transfer.Description,
        CompanyId = transfer.CompanyId,
        BranchId = transfer.BranchId,
        FiscalYearId = transfer.FiscalYearId,
        Lines = transfer.Lines.Select(l => new StockTransferLineDto
        {
            Id = l.Id,
            GoodsId = l.GoodsId,
            UnitId = l.UnitId,
            Quantity = l.Quantity,
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

public sealed class CreateStockTransferDto
{
    public string DocumentNumber { get; set; } = null!;

    public DateTime DocumentDate { get; set; }

    public Guid FromWarehouseId { get; set; }

    public Guid ToWarehouseId { get; set; }

    public string? Description { get; set; }

    public List<AddStockTransferLineDto> Lines { get; set; } = new();
}

public sealed class AddStockTransferLineDto
{
    public Guid GoodsId { get; set; }

    public Guid UnitId { get; set; }

    public decimal Quantity { get; set; }

    public string? Description { get; set; }
}

public sealed class StockTransferLineDto
{
    public Guid Id { get; set; }

    public Guid GoodsId { get; set; }

    public Guid UnitId { get; set; }

    public decimal Quantity { get; set; }

    public string? Description { get; set; }
}

public sealed class StockTransferDto
{
    public Guid Id { get; set; }

    public string DocumentNumber { get; set; } = null!;

    public DateTime DocumentDate { get; set; }

    public Guid FromWarehouseId { get; set; }

    public Guid ToWarehouseId { get; set; }

    public string Status { get; set; } = null!;

    public string? Description { get; set; }

    public Guid? CompanyId { get; set; }

    public Guid? BranchId { get; set; }

    public Guid? FiscalYearId { get; set; }

    public List<StockTransferLineDto> Lines { get; set; } = new();
}
