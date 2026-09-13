namespace RGRE.ERP.Domain.Inventory.Receipts;

public interface IReceiptRepository
{
    Task<Receipt?> GetAsync(
        Guid id,
        CancellationToken cancellationToken = default);

    Task AddAsync(
        Receipt receipt,
        CancellationToken cancellationToken = default);

    Task<IReadOnlyList<Receipt>> ListAsync(CancellationToken cancellationToken = default);
}
