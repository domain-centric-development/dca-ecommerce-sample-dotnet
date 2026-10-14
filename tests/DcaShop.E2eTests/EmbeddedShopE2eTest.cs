using System.Net;
using System.Net.Sockets;
using System.Text;

using Microsoft.Playwright;

namespace DcaShop.E2eTests;

/// <summary>
/// The shop inside someone else's iframe — a slide deck, a docs page, a demo — on another port of the same host.
///
/// Another port is a different <em>origin</em>, so the shop has to allow framing — which it does out of the box — but
/// the same <em>site</em>, so its <c>Lax</c> cookies travel into the frame unchanged; the case needs no configuration
/// at all. Framing from another site depends on how the shop was started; <c>EmbeddingPolicyE2eTest</c> starts the
/// shops for it.
///
/// The embedding page comes from a throwaway HTTP server the test starts on a free port of <c>localhost</c>. A real
/// origin rather than an intercepted one, because a browser refuses to frame anything on the local network from a
/// page whose own origin it could not resolve.
///
/// Same scenario as the Java sample's <c>EmbeddedShopE2ETest</c>.
/// </summary>
public sealed class EmbeddedShopE2eTest : BaseE2eTest, IDisposable
{
    private HttpListener? _embeddingServer;

    public EmbeddedShopE2eTest(BrowserFixture browser) : base(browser)
    {
    }

    [E2eFact(DisplayName = "A page on another port embeds the shop")]
    public async Task ASlideDeckOnAnotherPortFramesTheShopAndAddsToTheCart()
    {
        await Page.GotoAsync(StartEmbeddingServer("localhost", "/products"));

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
        var title = shop.Locator("[data-test='product-detail-title']");
        await Assertions.Expect(title).ToBeVisibleAsync();
        var product = (await title.TextContentAsync() ?? string.Empty).Trim();

        await shop.Locator("[data-test='product-add-to-cart-button']").ClickAsync();

        await Assertions.Expect(shop.Locator("[data-test='cart-item-name']")).ToHaveTextAsync([product]);
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