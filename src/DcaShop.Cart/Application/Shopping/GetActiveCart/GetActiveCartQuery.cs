namespace DcaShop.Cart.Application.Shopping.GetActiveCart;

/// <summary>Looks up the customer's active cart without creating one.</summary>
public sealed record GetActiveCartQuery(string CustomerId);
