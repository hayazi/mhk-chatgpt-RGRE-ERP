using RGRE.ERP.Application.Accounting;
using RGRE.ERP.Application.Inventory;
using RGRE.ERP.Application.Partners;
using RGRE.ERP.Domain.Inventory;

namespace RGRE.ERP.Web.Components.Services;

/// <summary>
/// Shared id → name lookups for the UI tables and pickers.
/// One instance per Blazor circuit; pages call <see cref="LoadAsync"/> and
/// <see cref="RefreshAsync"/> after mutations.
/// </summary>
public sealed class UiLookups
{
    private readonly IGoodsAppService _goodsService;
    private readonly IUnitAppService _unitService;
    private readonly IWarehouseAppService _warehouseService;
    private readonly IPartnerAppService _partnerService;
    private readonly ILedgerAccountAppService _accountService;

    public UiLookups(
        IGoodsAppService goodsService,
        IUnitAppService unitService,
        IWarehouseAppService warehouseService,
        IPartnerAppService partnerService,
        ILedgerAccountAppService accountService)
    {
        _goodsService = goodsService;
        _unitService = unitService;
        _warehouseService = warehouseService;
        _partnerService = partnerService;
        _accountService = accountService;
    }

    public bool Loaded { get; private set; }

    public IReadOnlyList<GoodsDto> Goods { get; private set; } = [];

    public IReadOnlyList<Unit> Units { get; private set; } = [];

    public IReadOnlyList<Warehouse> Warehouses { get; private set; } = [];

    public IReadOnlyList<PartnerDto> Partners { get; private set; } = [];

    public IReadOnlyList<LedgerAccountDto> Accounts { get; private set; } = [];

    public async Task LoadAsync(CancellationToken cancellationToken = default)
    {
        Goods = await _goodsService.ListAsync(cancellationToken);
        Units = await _unitService.ListAsync(cancellationToken);
        Warehouses = await _warehouseService.ListAsync(cancellationToken);
        Partners = await _partnerService.ListAsync(cancellationToken);
        Accounts = await _accountService.ListAsync(cancellationToken);

        Loaded = true;
    }

    public Task RefreshAsync(CancellationToken cancellationToken = default) => LoadAsync(cancellationToken);

    public string GoodsName(Guid goodsId)
        => Goods.FirstOrDefault(g => g.Id == goodsId)?.Name ?? "(unknown goods)";

    public string UnitName(Guid unitId)
        => Units.FirstOrDefault(u => u.Id == unitId)?.Name ?? "(unknown unit)";

    public string WarehouseName(Guid warehouseId)
        => Warehouses.FirstOrDefault(w => w.Id == warehouseId)?.Name ?? "(unknown warehouse)";

    public string PartnerName(Guid? partnerId)
    {
        if (partnerId is null)
        {
            return "—";
        }

        return Partners.FirstOrDefault(p => p.Id == partnerId)?.Name ?? "(unknown partner)";
    }

    public string AccountName(Guid accountId)
        => Accounts.FirstOrDefault(a => a.Id == accountId)?.Name ?? "(unknown account)";
}
