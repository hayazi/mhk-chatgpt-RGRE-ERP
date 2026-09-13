namespace RGRE.ERP.Domain.Accounting;

public sealed class VoucherLine
{
    public Guid Id { get; private set; }

    public Guid VoucherId { get; private set; }

    public Guid AccountId { get; private set; }

    public decimal Debit { get; private set; }

    public decimal Credit { get; private set; }

    public string? Description { get; private set; }

    /// <summary>Legacy tafzili-style dimensions.</summary>
    public Guid? PartnerId { get; private set; }

    public Guid? WarehouseId { get; private set; }

    internal VoucherLine(
        Guid accountId,
        decimal debit,
        decimal credit,
        string? description,
        Guid? partnerId,
        Guid? warehouseId)
    {
        if (accountId == Guid.Empty)
            throw new ArgumentException("Account is required.");

        Id = Guid.NewGuid();
        AccountId = accountId;
        Debit = debit;
        Credit = credit;
        Description = description;
        PartnerId = partnerId;
        WarehouseId = warehouseId;
    }

    private VoucherLine()
    {
    }
}
