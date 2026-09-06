namespace DcaShop.Checkout.Application.SyncCheckoutWithCart;

/// <summary><see cref="SessionId"/> is null when the cart had no active checkout session — then nothing was synced.</summary>
public sealed record SyncCheckoutWithCartResult(Guid? SessionId, int ItemCount)
{
    public static SyncCheckoutWithCartResult NoActiveSession() => new(null, 0);

    public static SyncCheckoutWithCartResult Synced(Guid sessionId, int itemCount) => new(sessionId, itemCount);

    public bool WasSynced => SessionId is not null;
}
