namespace DcaShop.Cart.Domain.Model;

/// <summary>Lifecycle of a shopping cart: modifiable while <see cref="Active"/>; <see cref="Completed"/> is a legacy, readable whole-cart state.</summary>
public enum CartStatus
{
    Active,
    Completed,
    Abandoned,
}
