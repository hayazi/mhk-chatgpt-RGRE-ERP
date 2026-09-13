namespace RGRE.ERP.Application.Inventory.Receipts;

public sealed class CreateReceiptDto
{
    public string DocumentNumber { get; set; } = null!;

    public DateTime DocumentDate { get; set; }

    public Guid WarehouseId { get; set; }

    public Guid? SupplierId { get; set; }

    public string? Description { get; set; }

    public List<AddReceiptLineDto> Lines { get; set; } = new();
}

public sealed class AddReceiptLineDto
{
    public Guid GoodsId { get; set; }

    public Guid UnitId { get; set; }

    public decimal Quantity { get; set; }

    public decimal UnitPrice { get; set; }

    public string? Description { get; set; }
}

public sealed class ReceiptLineDto
{
    public Guid Id { get; set; }

    public Guid GoodsId { get; set; }

    public Guid UnitId { get; set; }

    public decimal Quantity { get; set; }

    public decimal UnitPrice { get; set; }

    public decimal Amount { get; set; }

    public string? Description { get; set; }
}

public sealed class ReceiptDto
{
    public Guid Id { get; set; }

    public string DocumentNumber { get; set; } = null!;

    public DateTime DocumentDate { get; set; }

    public Guid WarehouseId { get; set; }

    public Guid? SupplierId { get; set; }

    public string Status { get; set; } = null!;

    public string? Description { get; set; }

    public Guid? CompanyId { get; set; }

    public Guid? BranchId { get; set; }

    public Guid? FiscalYearId { get; set; }

    public List<ReceiptLineDto> Lines { get; set; } = new();
}
