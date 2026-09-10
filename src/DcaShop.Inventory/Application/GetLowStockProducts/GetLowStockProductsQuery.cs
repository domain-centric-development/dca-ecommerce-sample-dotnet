namespace DcaShop.Inventory.Application.GetLowStockProducts;

/// <summary>Asks for the products whose available quantity is below the given threshold.</summary>
public sealed record GetLowStockProductsQuery(int Threshold);
