namespace DcaShop.E2eTests;

/// <summary>
/// The shop the suite drives as it is after a restart: a second shop beside it, started with the same configuration
/// and therefore the same signing secret, but with every store empty. A browser keeps its cookies across the two,
/// because cookies tell hosts apart, not ports.
/// </summary>
public static class RestartedShop
{
    public static SideShop Start() => SideShop.Start();
}