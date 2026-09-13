namespace RGRE.ERP.Domain.Inventory.Transfers.Events;

public sealed record StockTransferredDomainEvent(
    Guid TransferId,
    string DocumentNumber,
    DateTime DocumentDate,
    Guid FromWarehouseId,
    Guid ToWarehouseId,
    int LineCount);
