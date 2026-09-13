using RGRE.ERP.Application.Abstractions;
using RGRE.ERP.Domain.Partners;

namespace RGRE.ERP.Application.Partners;

public sealed class CreatePartnerDto
{
    public string Code { get; set; } = null!;
    public string Name { get; set; } = null!;
    public string? Name2 { get; set; }

    /// <summary>"Customer", "Supplier" or "Customer,Supplier".</summary>
    public string Kind { get; set; } = "Customer";

    public string? NationalId { get; set; }
    public string? EconomicCode { get; set; }
    public string? Telephone { get; set; }
    public string? Email { get; set; }
    public string? Address { get; set; }
    public decimal? CashCreditLimit { get; set; }
    public decimal? ChequeCreditLimit { get; set; }
    public string? Description { get; set; }
}

public sealed class PartnerDto
{
    public Guid Id { get; set; }
    public string Code { get; set; } = null!;
    public string Name { get; set; } = null!;
    public string? Name2 { get; set; }
    public string Kind { get; set; } = null!;
    public string? Telephone { get; set; }
    public string? Email { get; set; }
    public bool IsBlocked { get; set; }
}

public interface IPartnerAppService
{
    Task<Guid> CreateAsync(CreatePartnerDto input, CancellationToken cancellationToken = default);

    Task<IReadOnlyList<PartnerDto>> ListAsync(CancellationToken cancellationToken = default);

    Task BlockAsync(Guid id, CancellationToken cancellationToken = default);

    Task UnblockAsync(Guid id, CancellationToken cancellationToken = default);
}

public sealed class PartnerAppService : IPartnerAppService
{
    private readonly IPartnerRepository _partners;
    private readonly IUnitOfWork _unitOfWork;

    public PartnerAppService(IPartnerRepository partners, IUnitOfWork unitOfWork)
    {
        _partners = partners;
        _unitOfWork = unitOfWork;
    }

    public async Task<Guid> CreateAsync(CreatePartnerDto input, CancellationToken cancellationToken = default)
    {
        var kind = ParseKind(input.Kind);

        var partner = new Partner(
            input.Code,
            input.Name,
            kind,
            input.Name2,
            input.NationalId,
            input.EconomicCode,
            input.Telephone,
            input.Email,
            input.Address,
            input.CashCreditLimit,
            input.ChequeCreditLimit,
            input.Description);

        await _partners.AddAsync(partner, cancellationToken);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        return partner.Id;
    }

    public async Task<IReadOnlyList<PartnerDto>> ListAsync(CancellationToken cancellationToken = default)
    {
        var list = await _partners.ListAsync(cancellationToken);

        return list.Select(p => new PartnerDto
        {
            Id = p.Id,
            Code = p.Code,
            Name = p.Name,
            Name2 = p.Name2,
            Kind = p.Kind.ToString(),
            Telephone = p.Telephone,
            Email = p.Email,
            IsBlocked = p.IsBlocked,
        }).ToList();
    }

    public async Task BlockAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var partner = await GetOrThrowAsync(id, cancellationToken);
        partner.Block();
        await _unitOfWork.SaveChangesAsync(cancellationToken);
    }

    public async Task UnblockAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var partner = await GetOrThrowAsync(id, cancellationToken);
        partner.Unblock();
        await _unitOfWork.SaveChangesAsync(cancellationToken);
    }

    private async Task<Partner> GetOrThrowAsync(Guid id, CancellationToken cancellationToken)
        => await _partners.GetAsync(id, cancellationToken)
            ?? throw new KeyNotFoundException($"Partner '{id}' was not found.");

    private static PartnerKind ParseKind(string kind)
    {
        var result = PartnerKind.None;

        foreach (var part in kind.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries))
        {
            result |= part.ToLowerInvariant() switch
            {
                "customer" => PartnerKind.Customer,
                "supplier" => PartnerKind.Supplier,
                _ => throw new ArgumentException($"Unknown partner kind '{part}'."),
            };
        }

        return result;
    }
}
