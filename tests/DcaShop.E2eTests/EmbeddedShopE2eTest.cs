using Microsoft.Playwright;

namespace DcaShop.E2eTests;

/// <summary>
/// The shop inside an iframe on another origin — the case the SameSite and frame-options switch exists for.
///
/// The embedding page is the shop's own landing page reached under a second name for the same server, with an iframe
/// put into it by the test. No second server is needed. The two names are different sites to the browser, so the
/// frame is cross-site exactly as a real foreign host would be, and both stay inside the local network: a page on a
/// public domain may not frame localhost at all (private network access), which would hide the very behaviour under
/// test.
///
/// That second name defaults to <c>127.0.0.1</c> where the shop under test is <c>localhost</c>. Anywhere else — a
/// shop reached by service name in a container network, say — it has to be given:
/// <c>E2E_OTHER_ORIGIN_BASE_URL=http://shop-dotnet-other-origin:8080</c>. Without a second name these tests skip
/// rather than quietly run same-origin and prove nothing.
///
/// Two deployments, two expectations: the normal shop refuses to be framed and says so in <c>X-Frame-Options</c>;
/// the embedded shop (<c>Jwt__SameSite=None Jwt__SecureCookies=true</c>, behind TLS) renders in the frame and a form
/// POST from inside it reaches the cart — which is what fails while any of its cookies stays same-site. The second
/// runs only against a shop started that way: <c>E2E_EMBEDDED=true</c>.
///
/// Same scenario as the Java sample's <c>EmbeddedShopE2ETest</c>.
/// </summary>
public sealed class EmbeddedShopE2eTest : BaseE2eTest
{
    /// <summary>The shop's own address under a second name — a different site to the browser, the same server.</summary>
    internal static string OtherOriginUrl =>
        Environment.GetEnvironmentVariable("E2E_OTHER_ORIGIN_BASE_URL")
        ?? (BaseUrl.Contains("localhost", StringComparison.Ordinal)
            ? BaseUrl.Replace("localhost", "127.0.0.1", StringComparison.Ordinal)
            : string.Empty);

    public EmbeddedShopE2eTest(BrowserFixture browser) : base(browser)
    {
    }

    /// <summary>
    /// The embedded shop is reached over TLS, and locally that means the development certificate — issued for
    /// <c>localhost</c>, so the browser rejects it under the second name this test needs. Accepting it here says
    /// nothing about the shop; the certificate is not what is under test.
    /// </summary>
    protected override BrowserNewContextOptions? ContextOptions => new() { IgnoreHTTPSErrors = true };

    [EmbeddedModeFact(embedded: false)]
    public async Task ANormalShopRefusesToRenderInsideAFrameOnAnotherOrigin()
    {
        await OpenEmbeddingPageAsync();
        var framedPage = Page.WaitForResponseAsync(response => response.Url == BaseUrl + "/products");

        await FrameTheShopAsync("/products");

        var headers = await (await framedPage).AllHeadersAsync();
        Assert.Equal("SAMEORIGIN", headers["x-frame-options"]);
        Assert.False(
            await FramedShop().Locator("[data-test='product-card']").First.IsVisibleAsync(),
            "the shop must not render inside a foreign frame");
    }

    [EmbeddedModeFact(embedded: true)]
    public async Task AnEmbeddedShopAcceptsAFormPostMadeFromInsideTheForeignFrame()
    {
        await OpenEmbeddingPageAsync();
        await FrameTheShopAsync("/products");

        var shop = FramedShop();
        var firstProduct = shop.Locator("[data-test='view-product']").First;
        await firstProduct.WaitForAsync();
        await firstProduct.ClickAsync();

        // Adding to the cart is a form POST carrying the antiforgery token. It arrives complete only when the
        // identity cookie and the token cookie both travel into the frame.
        await shop.Locator("[data-test='product-add-to-cart-button']").ClickAsync();

        var cartItems = shop.Locator("[data-test='cart-item']");
        await cartItems.First.WaitForAsync();
        Assert.True(await cartItems.CountAsync() >= 1, "the product reached the cart of the framed shop");
    }

    /// <summary>Opens a page on the other origin — any page of it will do, it only has to host the frame.</summary>
    private Task OpenEmbeddingPageAsync() => Page.GotoAsync(OtherOriginUrl + "/");

    /// <summary>Puts the shop into a frame of that page, the way a foreign site would embed it.</summary>
    private Task FrameTheShopAsync(string shopPath) => Page.EvaluateAsync(
        """
        src => {
            const frame = document.createElement('iframe');
            frame.id = 'shop';
            frame.src = src;
            frame.width = 1000;
            frame.height = 800;
            document.body.appendChild(frame);
        }
        """,
        BaseUrl + shopPath);

    private IFrameLocator FramedShop() => Page.FrameLocator("#shop");
}
