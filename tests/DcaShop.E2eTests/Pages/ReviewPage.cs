using Microsoft.Playwright;

namespace DcaShop.E2eTests.Pages;

public sealed class ReviewPage : BasePage
{
    private const string UrlPattern = "/checkout/review";
    private const string PlaceOrderButton = "review-place-order-button";

    private ReviewPage(IPage page) : base(page)
    {
    }

    public static async Task<ReviewPage> OpenAsync(IPage page)
    {
        var review = new ReviewPage(page);
        await review.WaitForUrlAsync(UrlPattern);
        return review;
    }

    public Task<bool> ShowsEmailAsync(string email) => PageContainsAsync(email);

    public Task<bool> ShowsAddressAsync(string address) => PageContainsAsync(address);

    public async Task<ConfirmationPage> PlaceOrderAsync()
    {
        await ClickAsync(PlaceOrderButton);
        return await ConfirmationPage.OpenAsync(Page);
    }
}
