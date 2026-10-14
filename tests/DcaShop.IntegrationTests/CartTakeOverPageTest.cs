using System.Net;
using System.Text.RegularExpressions;

using DcaShop.Cart.Application.CartRecovery.GetCartMergeOptions;
using DcaShop.Cart.Application.CartRecovery.RecoverCart;

using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.DependencyInjection;

namespace DcaShop.IntegrationTests;

/// <summary>
/// The page that follows a login with a guest cart and an empty account cart: opening it moves no cart — the take-over
/// waits for the form the browser sends. The page's translation alone, against input ports that answer what the test
/// tells them.
/// </summary>
public sealed class CartTakeOverPageTest : IClassFixture<WebApplicationFactory<Program>>
{
    private readonly WebApplicationFactory<Program> _factory;

    public CartTakeOverPageTest(WebApplicationFactory<Program> factory) => _factory = factory;

    [Fact]
    public async Task OpeningThePageAfterTheLoginOffersTheTakeOverAndMovesNoCart()
    {
        var recoveries = new RecordingRecovery();
        using var shop = ShopWhereOnlyTheGuestCartHoldsItems(recoveries);
        var browser = GuestOrders.Guest(shop);

        var opened = await browser.GetAsync($"/cart/merge?anonymousUserId=guest-{Guid.NewGuid()}");

        Assert.Equal(HttpStatusCode.OK, opened.StatusCode);
        var page = await opened.Content.ReadAsStringAsync();
        Assert.Equal("Bringing your cart along.", AvailabilityPages.Text(page, @"data-test=""take-over-message"">([^<]*)</p>"));
        Assert.Equal("Continue", AvailabilityPages.Text(page, @"data-test=""take-over-continue-button"">([^<]*)</button>"));
        Assert.Empty(recoveries.Commands);
    }

    /// <summary>The shop whose merge options answer a take-over, with a recovery that only records what it is sent.</summary>
    private WebApplicationFactory<Program> ShopWhereOnlyTheGuestCartHoldsItems(RecordingRecovery recoveries) =>
        _factory.WithWebHostBuilder(builder => builder.ConfigureTestServices(s =>
        {
            s.AddSingleton<IGetCartMergeOptionsInputPort>(new TakeOverOptions());
            s.AddSingleton<IRecoverCartInputPort>(recoveries);
        }));

    private sealed class TakeOverOptions : IGetCartMergeOptionsInputPort
    {
        public Task<GetCartMergeOptionsResult> ExecuteAsync(GetCartMergeOptionsQuery query, CancellationToken cancellationToken = default) =>
            Task.FromResult(GetCartMergeOptionsResult.TakeOver());
    }

    private sealed class RecordingRecovery : IRecoverCartInputPort
    {
        public List<RecoverCartCommand> Commands { get; } = [];

        public Task<RecoverCartResult> ExecuteAsync(RecoverCartCommand command, CancellationToken cancellationToken = default)
        {
            Commands.Add(command);
            return Task.FromResult(RecoverCartResult.NoRecoveryNeeded(command.RegisteredUserId));
        }
    }
}

/// <summary>The form of the page that follows a login with a guest cart, sent as the browser sends it.</summary>
internal static class TakeOverForm
{
    private static readonly Regex Action = new(@"<form [^>]*action=""([^""]+)""[^>]*data-test=""take-over-form""", RegexOptions.Compiled);
    private static readonly Regex HiddenField = new(@"<input (?=[^>]*type=""hidden"")[^>]*name=""([^""]+)""[^>]*value=""([^""]*)""", RegexOptions.Compiled);

    /// <summary>Sends the take-over form of <paramref name="takeOverPage"/>, every hidden field of it; answers the response.</summary>
    public static Task<HttpResponseMessage> SendAsync(HttpClient browser, string takeOverPage)
    {
        var action = Action.Match(takeOverPage);
        Assert.True(action.Success, "the page has the take-over form");
        var form = AvailabilityPages.Section(takeOverPage, "take-over-form");
        form = form[..form.IndexOf("</form>", StringComparison.Ordinal)];
        var fields = HiddenField.Matches(form)
            .Select(m => new KeyValuePair<string, string>(m.Groups[1].Value, WebUtility.HtmlDecode(m.Groups[2].Value)));
        return browser.PostAsync(WebUtility.HtmlDecode(action.Groups[1].Value), new FormUrlEncodedContent(fields));
    }
}