namespace RGRE.ERP.Domain.Accounting;

public interface ILedgerAccountRepository
{
    Task<LedgerAccount?> GetAsync(Guid id, CancellationToken cancellationToken = default);

    Task<LedgerAccount?> GetByCodeAsync(string code, CancellationToken cancellationToken = default);

    Task<IReadOnlyList<LedgerAccount>> ListAsync(CancellationToken cancellationToken = default);

    Task AddAsync(LedgerAccount account, CancellationToken cancellationToken = default);
}

public interface IVoucherRepository
{
    Task<Voucher?> GetAsync(Guid id, CancellationToken cancellationToken = default);

    Task<IReadOnlyList<Voucher>> ListAsync(CancellationToken cancellationToken = default);

    Task AddAsync(Voucher voucher, CancellationToken cancellationToken = default);
}
