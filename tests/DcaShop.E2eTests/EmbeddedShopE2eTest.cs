using System.Net;
using System.Net.Sockets;
using System.Text;
using Microsoft.Playwright;

namespace DcaShop.E2eTests;

/// <summary>
/// The shop inside someone else's iframe — a slide deck, a docs page, a demo.
///
/// Two cases, and the difference is what "someone else" means to the browser. Another port of the same host is a
/// different <em>origin</em>, so the shop has to allow framing — which it does out of the box — but the same
/// <em>site</em>, so its <c>Lax</c> cookies travel into the frame unchanged; that is the slide-deck case and needs no
/// configuration at all. Another site — <c>127.0.0.1</c> against <c>localhost</c> here — leaves the cookies behind
/// unless the shop is started for it (<c>Jwt__SameSite=None Jwt__SecureCookies=true</c>, behind TLS), so that case
/// runs only against such a shop and skips otherwise.
///
/// Both embedding pages come from a throwaway HTTP server the test starts on a free port, bound to <c>localhost</c>
/// for the first case and to <c>127.0.0.1</c> for the second. A real origin rather than an intercepted one, because a
/// browser refuses to frame anything on the local network from a page whose own origin it could not resolve.
///
/// Same scenarios as the Java sample's <c>EmbeddedShopE2ETest</c>.
/// </summary>
public sealed class EmbeddedShopE2eTest : BaseE2eTest, IDisposable
{
    private HttpListener? _embeddingServer;

    public EmbeddedShopE2eTest(BrowserFixture browser) : base(browser)
    {
    }

    /// <summary>
    /// The cross-site shop is reached over TLS, and locally that means the development certificate — issued for
    /// <c>localhost</c>, so the browser rejects it under the second name this test needs. Accepting it here says
    /// nothing about the shop; the certificate is not what is under test.
    /// </summary>
    protected override BrowserNewContextOptions? ContextOptions => new() { IgnoreHTTPSErrors = true };

    [EmbeddedModeFact(embedded: false, DisplayName = "A slide deck on another port frames the shop and adds to the cart")]
    public async Task ASlideDeckOnAnotherPortFramesTheShopAndAddsToTheCart()
    {
        await Page.GotoAsync(StartEmbeddingServer("localhost", "/products"));

        await AddFirstProductToTheCartInTheFrameAsync();
    }

    [EmbeddedModeFact(embedded: true, DisplayName = "A page on another site frames the shop and adds to the cart")]
    public async Task APageOnAnotherSiteFramesTheShopAndAddsToTheCart()
    {
        // 127.0.0.1 is the same machine under a name the browser counts as a different site — which is what makes
        // the shop's cookies cross-site here, unlike the port-only difference above.
        await Page.GotoAsync(StartEmbeddingServer("127.0.0.1", "/products"));

        await AddFirstProductToTheCartInTheFrameAsync();
    }

    /// <summary>
    /// The whole point of framing the shop: the visitor can still use it. Adding to the cart is a form POST carrying
    /// the antiforgery token, so it only arrives complete when the identity cookie and the token cookie both
    /// travelled into the frame.
    /// </summary>
    private async Task AddFirstProductToTheCartInTheFrameAsync()
    {
        var shop = Page.FrameLocator("#shop");
        var firstProduct = shop.Locator("[data-test='view-product']").First;
        await firstProduct.WaitForAsync();
        await firstProduct.ClickAsync();

        await shop.Locator("[data-test='product-add-to-cart-button']").ClickAsync();

        var cartItems = shop.Locator("[data-test='cart-item']");
        await cartItems.First.WaitForAsync();
        Assert.True(await cartItems.CountAsync() >= 1, "the product reached the cart of the framed shop");
    }

    /// <summary>A throwaway server on a free port of this host, serving nothing but the embedding page.</summary>
    /// <returns>The address of that page.</returns>
    private string StartEmbeddingServer(string host, string shopPath)
    {
        var address = $"http://{host}:{FreePort()}/";
        _embeddingServer = new HttpListener();
        _embeddingServer.Prefixes.Add(address);
        _embeddingServer.Start();

        var page = Encoding.UTF8.GetBytes(
            $"<!doctype html>\n<title>A page that frames the shop</title>\n"
            + $"<iframe id=\"shop\" src=\"{BaseUrl}{shopPath}\" width=\"1000\" height=\"800\"></iframe>\n");

        _ = Task.Run(async () =>
        {
            while (_embeddingServer?.IsListening == true)
            {
                HttpListenerContext context;
                try
                {
                    context = await _embeddingServer.GetContextAsync();
                }
                catch (Exception)
                {
                    return; // the listener was closed with the test
                }

                context.Response.ContentType = "text/html; charset=utf-8";
                context.Response.ContentLength64 = page.Length;
                await context.Response.OutputStream.WriteAsync(page);
                context.Response.Close();
            }
        });

        return address;
    }

    private static int FreePort()
    {
        var probe = new TcpListener(IPAddress.Loopback, 0);
        probe.Start();
        var port = ((IPEndPoint)probe.LocalEndpoint).Port;
        probe.Stop();
        return port;
    }

    public void Dispose()
    {
        _embeddingServer?.Close();
        _embeddingServer = null;
    }
}
