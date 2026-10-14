namespace DcaShop.E2eTests;

/// <summary>
/// An E2E test that starts a <see cref="SideShop"/> beside the suite's. Only a suite that starts shops itself can;
/// against the one <c>E2E_BASE_URL</c> names it skips instead of failing.
/// </summary>
public abstract class SideShopFactAttribute : FactAttribute
{
    protected SideShopFactAttribute(string reason)
    {
        if (ShopUnderTest.StartedElsewhere)
        {
            Skip = reason;
        }
    }
}

/// <summary>
/// An E2E test that starts a shop of its own: with services the suite's shop does not have, or with the seeded catalogue
/// and stock as the shop starts with them, which the suite's shared shop does not keep.
/// </summary>
public sealed class OwnShopFactAttribute : SideShopFactAttribute
{
    public OwnShopFactAttribute()
        : base("starts a shop of its own, which only a suite without E2E_BASE_URL does")
    {
    }
}

/// <summary>An E2E test that restarts the shop.</summary>
public sealed class RestartingFactAttribute : SideShopFactAttribute
{
    public RestartingFactAttribute()
        : base("restarts the shop, which only a shop the suite started itself allows (no E2E_BASE_URL)")
    {
    }
}