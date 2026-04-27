namespace MoneyKey.Core.DTOs.Lists;

/// <summary>
/// Stored in ListItem.ItemData for inventory items.
/// </summary>
public record InventoryItemData(
    string? Category = null,    // "Elektronik", "Möbler" etc
    string? Location = null,    // "Hemmakontoret", "Källaren"
    decimal? Value = null,    // Inköpspris
    decimal? CurrentValue = null,    // Nuvarande uppskattad värde (om annan)
    string? PurchaseDate = null,    // ISO date
    string? WarrantyEnd = null,    // ISO date — null = ingen garanti
    int? LinkedReceiptId = null,   // FK till ReceiptBatch.Id
    string? SerialNumber = null,
    string? Brand = null,
    string? Model = null,
    string? Notes = null,
    string? Condition = "good"  // "new" | "good" | "fair" | "poor"
);