namespace RGRE.ERP.Domain.Inventory.Receipts;

/// <summary>
/// Mirrors the workflow of the legacy <c>Resid</c> documents.
/// </summary>
public enum ReceiptStatus
{
    Draft = 0,
    Posted = 1,
    Cancelled = 2,
}

/// <summary>
/// Mirrors the workflow of the legacy <c>Khorooj</c> documents.
/// </summary>
public enum IssueStatus
{
    Draft = 0,
    Posted = 1,
    Cancelled = 2,
}

/// <summary>
/// Mirrors the workflow of the legacy <c>EnteghalBeinAnbar</c> documents.
/// </summary>
public enum TransferStatus
{
    Draft = 0,
    Posted = 1,
    Cancelled = 2,
}
