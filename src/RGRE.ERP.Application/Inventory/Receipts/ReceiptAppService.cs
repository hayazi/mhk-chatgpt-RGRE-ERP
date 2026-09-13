using RGRE.ERP.Application.Abstractions;
using RGRE.ERP.Application.Inventory;
using RGRE.ERP.Application.Platform;
using RGRE.ERP.Domain.Accounting;
using RGRE.ERP.Domain.Inventory;
using RGRE.ERP.Domain.Inventory.Receipts;

namespace RGRE.ERP.Application.Inventory.Receipts;

public sealed class ReceiptAppService : IReceiptAppService
{
    private readonly IReceiptRepository _receipts;
    private readonly IGoodsRepository _goods;
    private readonly IStockLedgerService _ledger;
    private readonly IUnitOfWork _unitOfWork;
    private readonly IFiscalYearRepository _fiscalYears;
    private readonly ICurrentUserContext _userContext;

    public ReceiptAppService(
        IReceiptRepository receipts,
        IGoodsRepository goods,
        IStockLedgerService ledger,
        IUnitOfWork unitOfWork,
        IFiscalYearRepository fiscalYears,
        ICurrentUserContext userContext)
    {
        _receipts = receipts;
        _goods = goods;
        _ledger = ledger;
        _unitOfWork = unitOfWork;
        _fiscalYears = fiscalYears;
        _userContext = userContext;
    }

    public async Task<ReceiptDto?> GetAsync(
        Guid id,
        CancellationToken cancellationToken = default)
    {
        var receipt = await _receipts.GetAsync(id, cancellationToken);

        return receipt is null ? null : ToDto(receipt);
    }

    public async Task<IReadOnlyList<ReceiptDto>> ListAsync(
        CancellationToken cancellationToken = default)
    {
        var list = await _receipts.ListAsync(cancellationToken);

        return list.Select(ToDto).ToList();
    }

    public async Task<Guid> CreateAsync(
        CreateReceiptDto input,
        CancellationToken cancellationToken = default)
    {
        var receipt = Receipt.Create(
            input.DocumentNumber,
            input.DocumentDate,
            input.WarehouseId,
            input.SupplierId,
            input.Description);

        foreach (var line in input.Lines)
        {
            await EnsureGoodsExistsAsync(line.GoodsId, cancellationToken);
            receipt.AddLine(
                line.GoodsId,
                line.UnitId,
                line.Quantity,
                line.UnitPrice,
                line.Description);
        }

        await DocumentContextRules.StampCreationContextAsync(_fiscalYears, _userContext, receipt, cancellationToken);

        await _receipts.AddAsync(receipt, cancellationToken);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        return receipt.Id;
    }

    public async Task AddLineAsync(
        Guid receiptId,
        AddReceiptLineDto input,
        CancellationToken cancellationToken = default)
    {
        var receipt = await GetOrThrowAsync(receiptId, cancellationToken);

        await EnsureGoodsExistsAsync(input.GoodsId, cancellationToken);

        receipt.AddLine(
            input.GoodsId,
            input.UnitId,
            input.Quantity,
            input.UnitPrice,
            input.Description);

        await _unitOfWork.SaveChangesAsync(cancellationToken);
    }

    public async Task RemoveLineAsync(
        Guid receiptId,
        Guid lineId,
        CancellationToken cancellationToken = default)
    {
        var receipt = await GetOrThrowAsync(receiptId, cancellationToken);

        receipt.RemoveLine(lineId);

        await _unitOfWork.SaveChangesAsync(cancellationToken);
    }

    public async Task PostAsync(
        Guid receiptId,
        CancellationToken cancellationToken = default)
    {
        var receipt = await GetOrThrowAsync(receiptId, cancellationToken);

        await DocumentContextRules.EnsureFiscalYearAcceptsAsync(_fiscalYears, _userContext, receipt, cancellationToken);

        receipt.Post();

        await _ledger.ApplyReceiptAsync(receipt, cancellationToken);

        await _unitOfWork.SaveChangesAsync(cancellationToken);
    }

    public async Task CancelAsync(
        Guid receiptId,
        CancellationToken cancellationToken = default)
    {
        var receipt = await GetOrThrowAsync(receiptId, cancellationToken);

        receipt.Cancel();

        await _unitOfWork.SaveChangesAsync(cancellationToken);
    }

    private async Task<Receipt> GetOrThrowAsync(Guid id, CancellationToken cancellationToken)
    {
        return await _receipts.GetAsync(id, cancellationToken)
            ?? throw new KeyNotFoundException($"Receipt '{id}' was not found.");
    }

    private static ReceiptDto ToDto(Receipt receipt) => new()
    {
        Id = receipt.Id,
        DocumentNumber = receipt.DocumentNumber,
        DocumentDate = receipt.DocumentDate,
        WarehouseId = receipt.WarehouseId,            SupplierId = receipt.SupplierId,
            Status = receipt.Status.ToString(),
            Description = receipt.Description,
            CompanyId = receipt.CompanyId,
            BranchId = receipt.BranchId,
            FiscalYearId = receipt.FiscalYearId,
        Lines = receipt.Lines.Select(l => new ReceiptLineDto
        {
            Id = l.Id,
            GoodsId = l.ProductId,
            UnitId = l.UnitId,
            Quantity = l.Quantity,
            UnitPrice = l.UnitPrice,
            Amount = l.Amount,
            Description = l.Description,
        }).ToList(),
    };

    private async Task EnsureGoodsExistsAsync(Guid goodsId, CancellationToken cancellationToken)
    {
        _ = await _goods.GetAsync(goodsId, cancellationToken)
            ?? throw new KeyNotFoundException($"Goods '{goodsId}' was not found.");
    }
}
