using Microsoft.Playwright;

namespace DcaShop.E2eTests.Pages;

public sealed class BuyerInfoPage : BasePage
{
    private const string UrlPattern = "/checkout/buyer";
    private const string ContinueButton = "buyer-continue-button";
    private const string ErrorMessage = "buyer-error-message";
    private const string AuthOptions = "buyer-auth-options";
    private const string LoggedInBanner = "buyer-logged-in";
    private const string EmailInput = "buyer-email-input";

    private BuyerInfoPage(IPage page) : base(page)
    {
    }

    /// <summary>Opens the buyer step of the checkout the visitor already started.</summary>
    public static async Task<BuyerInfoPage> NavigateToAsync(IPage page)
    {
        await page.GotoAsync(BaseUrl + UrlPattern);
        return await OpenAsync(page);
    }

    public static async Task<BuyerInfoPage> OpenAsync(IPage page)
    {
        var buyer = new BuyerInfoPage(page);
        await buyer.WaitForUrlAsync(UrlPattern);
        return buyer;
    }

    public async Task<BuyerInfoPage> FillBuyerInfoAsync(string email, string firstName, string lastName, string phone)
    {
        await FillAsync("email", email);
        await FillAsync("firstName", firstName);
        await FillAsync("lastName", lastName);
        await FillAsync("phone", phone);
        return this;
    }

    public async Task<DeliveryPage> ContinueToDeliveryAsync()
    {
        await ClickAsync(ContinueButton);
        return await DeliveryPage.OpenAsync(Page);
    }

    public async Task<BuyerInfoPage> SubmitWithErrorsAsync()
    {
        await ClickAsync(ContinueButton);
        // Brief wait: HTML5 validation prevents submission (no navigation),
        // server-side validation re-renders this page.
        await Page.WaitForTimeoutAsync(500);
        return this;
    }

    public async Task<bool> HasValidationErrorsAsync() =>
        await ExistsAsync(ErrorMessage)
        || await PageContainsAsync("valid email")
        || await PageContainsAsync("error")
        || await HasHtml5ValidationErrorsAsync();

    private async Task<bool> HasHtml5ValidationErrorsAsync() =>
        await Page.EvaluateAsync<bool>("() => { const form = document.querySelector('[data-test=\"buyer-form\"]'); return form != null && !form.checkValidity(); }");

    /// <summary>The login/register prompt is offered to anonymous visitors only.</summary>
    public Task<bool> ShowsAuthOptionsAsync() => ExistsAsync(AuthOptions);

    /// <summary>The "Logged in as …" banner shown instead of the login/register prompt; empty when anonymous.</summary>
    public async Task<string> LoggedInBannerTextAsync() =>
        await ExistsAsync(LoggedInBanner)
            ? (await Page.Locator($"[data-test='{LoggedInBanner}']").TextContentAsync() ?? string.Empty).Trim()
            : string.Empty;

    /// <summary>The current value of the email input — prefilled from the identity for a registered visitor.</summary>
    public Task<string> EmailValueAsync() => Page.Locator($"[data-test='{EmailInput}']").InputValueAsync();

    /// <summary>The session's order summary as <c>name xquantity</c> pairs — the snapshot, not the current cart.</summary>
    public async Task<IReadOnlyList<string>> SummaryItemsAsync()
    {
        var items = Page.Locator("[data-test='order-summary-item']");
        var lines = new List<string>();
        for (var i = 0; i < await items.CountAsync(); i++)
        {
            var item = items.Nth(i);
            var name = (await item.Locator("[data-test='order-summary-item-name']").TextContentAsync() ?? string.Empty).Trim();
            var quantity = (await item.Locator("[data-test='order-summary-item-qty']").TextContentAsync() ?? string.Empty).Trim();
            lines.Add($"{name} {quantity}");
        }
        return lines;
    }

    /// <summary>The values the form's four fields hold, in form order: email, first name, last name, phone.</summary>
    public async Task<string[]> FieldValuesAsync()
    {
        var email = Page.Locator($"[data-test='{EmailInput}']");
        await Assertions.Expect(email).ToBeVisibleAsync();
        return new[]
        {
            await email.InputValueAsync(),
            await Page.Locator("[data-test='buyer-firstname-input']").InputValueAsync(),
            await Page.Locator("[data-test='buyer-lastname-input']").InputValueAsync(),
            await Page.Locator("[data-test='buyer-phone-input']").InputValueAsync(),
        };
    }

    public string ShownPath => CurrentPath;

    public bool IsOnPage => CurrentPath.Contains("/checkout/buyer", StringComparison.Ordinal);

    /// <summary>Presses "Continue to Delivery" without waiting for a page — the browser may hold the form back.</summary>
    public async Task PressContinueToDeliveryAsync()
    {
        var button = Page.Locator($"[data-test='{ContinueButton}']");
        await Assertions.Expect(button).ToBeVisibleAsync();
        await button.ClickAsync();
    }

    /// <summary>Submits the form and answers the path of whatever page the shop opens next, without expecting one.</summary>
    public async Task<string> SubmitAndShowNextPathAsync()
    {
        var button = Page.Locator($"[data-test='{ContinueButton}']");
        await Assertions.Expect(button).ToBeVisibleAsync();
        await Page.RunAndWaitForRequestFinishedAsync(
            () => button.ClickAsync(),
            new PageRunAndWaitForRequestFinishedOptions { Predicate = request => request.IsNavigationRequest && request.Method == "GET" });
        await Page.WaitForLoadStateAsync(LoadState.DOMContentLoaded);
        return CurrentPath;
    }

    /// <summary>
    /// Sends a delivery form to the shop from this page, with this page's form token — what a second tab or a stale
    /// page of the same browser would send — and answers the path of the page the shop shows in the end.
    /// </summary>
    public async Task<string> SendDeliveryFormAsync(string street, string city, string postalCode, string country, string shippingOptionId)
    {
        var form = Page.Locator("[data-test='buyer-form']");
        await Assertions.Expect(form).ToBeVisibleAsync();
        var fields = new Dictionary<string, string>
        {
            ["street"] = street,
            ["streetLine2"] = string.Empty,
            ["city"] = city,
            ["state"] = string.Empty,
            ["postalCode"] = postalCode,
            ["country"] = country,
            ["shippingOptionId"] = shippingOptionId,
        };
        await MarkDocumentAsync();
        await Page.EvaluateAsync("""
            fields => {
              const token = document.querySelector("[data-test='buyer-form'] input[name='__RequestVerificationToken']").value;
              const sent = document.createElement('form');
              sent.method = 'post';
              sent.action = '/checkout/delivery';
              for (const [name, value] of Object.entries({ ...fields, __RequestVerificationToken: token })) {
                const input = document.createElement('input');
                input.type = 'hidden';
                input.name = name;
                input.value = value;
                sent.appendChild(input);
              }
              document.body.appendChild(sent);
              sent.submit();
            }
            """, fields);
        // The shop's answer replaces the marked document, wherever its redirects lead
        await Page.WaitForFunctionAsync("() => window.__documentMark !== true && document.readyState !== 'loading'");
        return CurrentPath;
    }

    /// <summary>The names of the form's fields the browser's own checks refuse, in form order.</summary>
    public Task<string[]> FieldsTheBrowserRefusesAsync() =>
        Page.EvaluateAsync<string[]>("() => [...document.querySelectorAll('[data-test=\"buyer-form\"] input:not([type=hidden])')].filter(f => !f.checkValidity()).map(f => f.name)");
}