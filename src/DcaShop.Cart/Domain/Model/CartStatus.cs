namespace DcaShop.Cart.Domain.Model;

/// <summary>Lifecycle of a shopping cart: modifiable while <see cref="Active"/>, locked once <see cref="CheckedOut"/>.</summary>
public enum CartStatus
{
    Active,
    CheckedOut,
    Completed,
    Abandoned,
}
