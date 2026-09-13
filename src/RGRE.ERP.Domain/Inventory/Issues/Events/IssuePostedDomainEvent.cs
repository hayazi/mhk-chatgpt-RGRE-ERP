namespace RGRE.ERP.Domain.Inventory.Issues.Events;

public sealed record IssuePostedDomainEvent(
    Guid IssueId,
    string DocumentNumber,
    DateTime DocumentDate,
    Guid WarehouseId,
    decimal TotalAmount);
