namespace DcaShop.Inventory.Application.ReleaseStock;

/// <summary>The products whose reservation was given back; a product with nothing reserved is not among them.</summary>
public sealed record ReleaseStockResult(IReadOnlyList<Guid> Released);