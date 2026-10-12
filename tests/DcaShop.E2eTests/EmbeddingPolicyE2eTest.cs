using System.Net;
using System.Net.Sockets;
using System.Security.Cryptography;
using System.Security.Cryptography.X509Certificates;
using System.Text;

using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Hosting.Server;
using Microsoft.AspNetCore.Hosting.Server.Features;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Playwright;

namespace DcaShop.E2eTests;

/// <summary>
/// Who may frame the shop, decided by how it was started. The out-of-the-box shop keeps its cookies same-site, so a
/// page on another site frames it but cannot use it. Each shop here is started by the test itself on a port of its own
/// — the shared shop under test stays untouched.
/// </summary>
public sealed class EmbeddingPolicyE2eTest : BaseE2eTest, IDisposable
{
    private readonly List<WebApplicationFactory<Program>> _ownShops = [];
    private HttpListener? _embeddingServer;

    public EmbeddingPolicyE2eTest(BrowserFixture browser) : base(browser)
    {
    }

    /// <summary>The shop over TLS presents a certificate made up for this run; the certificate is not under test.</summary>
    protected override BrowserNewContextOptions? ContextOptions => new() { IgnoreHTTPSErrors = true };

    [E2eFact(DisplayName = "A foreign site cannot add to the cart")]
    public async Task APageOnAnotherSiteCannotPutAProductIntoTheFramedShopsCart()
    {
        // Out of the box, but over TLS: whether the cookies stay home is then the shop's policy alone, not a browser
        // dropping a Secure cookie that arrived over plain HTTP.
        var shopUrl = StartOwnShop(overTls: true);
        await Page.GotoAsync(StartEmbeddingServer("127.0.0.1", shopUrl + "/products"));

        await PressAddToCartOnTheFirstProductInTheFrameAsync();

        await ShowInTheFrameAsync(shopUrl + "/cart");
        var shop = Page.FrameLocator("#shop");
        await Assertions.Expect(shop.Locator(".cart__header-title")).ToHaveTextAsync("Shopping Cart");
        Assert.Equal(0, await shop.Locator("[data-test='cart-item']").CountAsync());
    }

    [E2eFact(DisplayName = "A shop started for embedding works cross site")]
    public async Task AShopStartedForEmbeddingTakesTheProductIntoItsCartInsideAForeignSitesFrame()
    {
        var shopUrl = StartOwnShop(overTls: true, ("Jwt:SameSite", "None"), ("Jwt:SecureCookies", "true"));
        await Page.GotoAsync(StartEmbeddingServer("127.0.0.1", shopUrl + "/products"));

        var product = await PressAddToCartOnTheFirstProductInTheFrameAsync();

        var cartItemNames = Page.FrameLocator("#shop").Locator("[data-test='cart-item-name']");
        await Assertions.Expect(cartItemNames).ToHaveTextAsync([product]);
    }

    [E2eFact(DisplayName = "A shop started without framing refuses the frame")]
    public async Task AShopStartedWithFramingSwitchedOffShowsNothingInsideAnotherPagesFrame()
    {
        var shopUrl = StartOwnShop(overTls: false, ("Jwt:AllowFraming", "false"));
        var framedPage = Page.WaitForResponseAsync(shopUrl + "/products");

        await Page.GotoAsync(StartEmbeddingServer("localhost", shopUrl + "/products"));

        Assert.Equal("SAMEORIGIN", await (await framedPage).HeaderValueAsync("x-frame-options"));
        Assert.Equal(0, await Page.FrameLocator("#shop").Locator("[data-test='view-product']").CountAsync());
    }

    /// <summary>
    /// Opens the first product inside the frame and presses "Add to Cart", returning once the shop has answered the
    /// add with a page — whichever it is. A navigation started before that answer would cancel the add.
    /// </summary>
    /// <returns>The product's name, as its page shows it.</returns>
    private async Task<string> PressAddToCartOnTheFirstProductInTheFrameAsync()
    {
        var shop = Page.FrameLocator("#shop");
        var firstProduct = shop.Locator("[data-test='view-product']").First;
        await Assertions.Expect(firstProduct).ToBeVisibleAsync();
        await firstProduct.ClickAsync();

        var title = shop.Locator("[data-test='product-detail-title']");
        await Assertions.Expect(title).ToBeVisibleAsync();
        var product = (await title.TextContentAsync() ?? string.Empty).Trim();

        await Page.RunAndWaitForResponseAsync(
            () => shop.Locator("[data-test='product-add-to-cart-button']").ClickAsync(),
            response => response.Request.IsNavigationRequest
                && new Uri(response.Url).AbsolutePath.StartsWith("/cart", StringComparison.Ordinal)
                && response.Status is < 300 or >= 400);
        return product;
    }

    /// <summary>
    /// Points the frame at another page of the shop and waits until that page has loaded — the frame may still show
    /// the page the last action led to, so a look before the load would read the old one.
    /// </summary>
    private async Task ShowInTheFrameAsync(string url)
    {
        var frame = Page.Frame("shop") ?? throw new InvalidOperationException("the embedding page has no frame named shop");
        await frame.GotoAsync(url);
    }

    /// <summary>
    /// A shop of this test's own, on a free port of this host, reached as <c>localhost</c>. Over TLS where its cookies
    /// are Secure: a browser drops a Secure cookie that did not arrive over HTTPS.
    /// </summary>
    private string StartOwnShop(bool overTls, params (string Key, string Value)[] settings)
    {
        var factory = new WebApplicationFactory<Program>().WithWebHostBuilder(builder =>
        {
            foreach (var (key, value) in settings)
            {
                builder.UseSetting(key, value);
            }
        });
        factory.UseKestrel(kestrel => kestrel.Listen(IPAddress.Loopback, 0, listen =>
        {
            if (overTls)
            {
                listen.UseHttps(CertificateForLocalhost());
            }
        }));
        _ownShops.Add(factory);
        factory.StartServer();

        var address = factory.Services.GetRequiredService<IServer>().Features.Get<IServerAddressesFeature>()!.Addresses.First();
        return $"{(overTls ? "https" : "http")}://localhost:{new Uri(address).Port}";
    }

    private static X509Certificate2 CertificateForLocalhost()
    {
        using var key = RSA.Create(2048);
        var request = new CertificateRequest("CN=localhost", key, HashAlgorithmName.SHA256, RSASignaturePadding.Pkcs1);
        var names = new SubjectAlternativeNameBuilder();
        names.AddDnsName("localhost");
        request.CertificateExtensions.Add(names.Build());
        return request.CreateSelfSigned(DateTimeOffset.UtcNow.AddDays(-1), DateTimeOffset.UtcNow.AddDays(1));
    }

    /// <summary>A throwaway server on a free port of this host, serving nothing but a page that frames the shop.</summary>
    /// <returns>The address of that page.</returns>
    private string StartEmbeddingServer(string host, string shopUrl)
    {
        var address = $"http://{host}:{FreePort()}/";
        _embeddingServer = new HttpListener();
        _embeddingServer.Prefixes.Add(address);
        _embeddingServer.Start();

        var page = Encoding.UTF8.GetBytes(
            "<!doctype html>\n<title>A page that frames the shop</title>\n"
            + $"<iframe id=\"shop\" name=\"shop\" src=\"{shopUrl}\" width=\"1000\" height=\"800\"></iframe>\n");

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
        foreach (var shop in _ownShops)
        {
            shop.Dispose();
        }
    }
}