namespace RGRE.ERP.Domain.Partners;

public interface IPartnerRepository
{
    Task<Partner?> GetAsync(Guid id, CancellationToken cancellationToken = default);

    Task<IReadOnlyList<Partner>> ListAsync(CancellationToken cancellationToken = default);

    Task AddAsync(Partner partner, CancellationToken cancellationToken = default);
}
