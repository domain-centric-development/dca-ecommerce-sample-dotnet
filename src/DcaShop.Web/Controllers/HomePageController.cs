using Microsoft.AspNetCore.Mvc;

namespace DcaShop.Web.Controllers;

/// <summary>Landing page. Lives in the host until a Portal context arrives (stage 2); the Java sample renders it from its Portal context.</summary>
public sealed class HomePageController : Controller
{
    [HttpGet("/")]
    public IActionResult Index() => View("~/Views/Home/Index.cshtml");
}
