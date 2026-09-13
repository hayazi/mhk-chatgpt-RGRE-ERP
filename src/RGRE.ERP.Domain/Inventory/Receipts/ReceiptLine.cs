namespace RGRE.ERP.Domain.Inventory.Receipts;

public sealed class ReceiptLine
{
    public Guid Id { get; private set; }

    public Guid ReceiptId { get; private set; }

    public Guid ProductId { get; private set; }

    public Guid UnitId { get; private set; }

    public decimal Quantity { get; private set; }

    public decimal UnitPrice { get; private set; }

    public decimal Amount { get; private set; }

    public string? Description { get; private set; }

    private ReceiptLine()
    {
    }

    internal ReceiptLine(
        Guid receiptId,
        Guid productId,
        Guid unitId,
        decimal quantity,
        decimal unitPrice,
        string? description)
    {
        if (productId == Guid.Empty)
            throw new ArgumentException("Product is required.");

        if (unitId == Guid.Empty)
            throw new ArgumentException("Unit is required.");

        if (quantity <= 0)
            throw new ArgumentException("Quantity must be greater than zero.");

        if (unitPrice < 0)
            throw new ArgumentException("Unit price cannot be negative.");

        Id = Guid.NewGuid();
        ReceiptId = receiptId;
        ProductId = productId;
        UnitId = unitId;
        Quantity = quantity;
        UnitPrice = unitPrice;
        Amount = quantity * unitPrice;
        Description = description;
    }
}
