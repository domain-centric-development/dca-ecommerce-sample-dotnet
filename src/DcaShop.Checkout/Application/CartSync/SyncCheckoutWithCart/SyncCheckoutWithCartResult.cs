namespace DcaShop.Checkout.Application.CartSync.SyncCheckoutWithCart;

/// <summary>Legacy response shape. Snapshot checkout always returns the no-synchronization result.</summary>
public sealed record SyncCheckoutWithCartResult(Guid? SessionId, int ItemCount)
{
    public static SyncCheckoutWithCartResult NoActiveSession() => new(null, 0);

    public static SyncCheckoutWithCartResult Synced(Guid sessionId, int itemCount) => new(sessionId, itemCount);

    public bool WasSynced => SessionId is not null;
}
