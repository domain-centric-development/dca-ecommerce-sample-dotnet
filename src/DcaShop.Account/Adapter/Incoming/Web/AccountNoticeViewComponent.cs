using Microsoft.AspNetCore.Mvc;

namespace DcaShop.Account.Adapter.Incoming.Web;

/// <summary>
/// The notice an account page left for a page of another context — the one a registration leads to — as a page
/// fragment the layout invokes by name (<c>Component.InvokeAsync("AccountNotice")</c>). It takes only the notice
/// addressed to the page being rendered, path and query, and renders nothing when there is none.
/// </summary>
public sealed class AccountNoticeViewComponent : ViewComponent
{
    public IViewComponentResult Invoke()
    {
        var page = $"{HttpContext.Request.Path}{HttpContext.Request.QueryString}";
        return TempData.TakeAccountPageNotice(AccountPageNotices.AddressedTo(page)) is { } notice
            ? View("Default", notice.Sentence())
            : Content(string.Empty);
    }
}