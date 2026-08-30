using Microsoft.AspNetCore.Mvc;

namespace DcaShop.Portal.Adapter.Incoming.Web;

/// <summary>The shop's landing page.</summary>
public sealed class HomePageController : Controller
{
    [HttpGet("/")]
    public IActionResult Index() => View("~/Views/Home/Index.cshtml");
}
