namespace RGRE.ERP.Domain.Inventory.Receipts.Events;

public sealed record ReceiptPostedDomainEvent(
    Guid ReceiptId,
    string DocumentNumber,
    DateTime DocumentDate,
    Guid WarehouseId,
    decimal TotalAmount);
