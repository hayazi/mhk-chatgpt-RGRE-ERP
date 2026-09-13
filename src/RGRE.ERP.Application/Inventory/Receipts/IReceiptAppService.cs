namespace RGRE.ERP.Application.Inventory.Receipts;

public interface IReceiptAppService
{
    Task<Guid> CreateAsync(
        CreateReceiptDto input,
        CancellationToken cancellationToken = default);

    Task<ReceiptDto?> GetAsync(
        Guid id,
        CancellationToken cancellationToken = default);

    Task<IReadOnlyList<ReceiptDto>> ListAsync(
        CancellationToken cancellationToken = default);

    Task AddLineAsync(
        Guid receiptId,
        AddReceiptLineDto input,
        CancellationToken cancellationToken = default);

    Task RemoveLineAsync(
        Guid receiptId,
        Guid lineId,
        CancellationToken cancellationToken = default);

    Task PostAsync(
        Guid receiptId,
        CancellationToken cancellationToken = default);

    Task CancelAsync(
        Guid receiptId,
        CancellationToken cancellationToken = default);
}
