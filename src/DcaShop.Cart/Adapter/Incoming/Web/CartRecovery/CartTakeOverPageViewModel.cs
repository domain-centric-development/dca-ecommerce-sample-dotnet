namespace DcaShop.Cart.Adapter.Incoming.Web.CartRecovery;

/// <summary>
/// What the take-over page carries back in its form: which identity the guest cart belongs to and where the visitor
/// was headed.
/// </summary>
public sealed record CartTakeOverPageViewModel(string AnonymousUserId, string? ReturnUrl);