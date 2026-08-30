using Microsoft.AspNetCore.Mvc;

namespace DcaShop.Portal.Adapter.Incoming.Web;

/// <summary>
/// Renders a view that fails on purpose, so the error page can be seen at work. It matches the Java sample's
/// Pug error demo; the route is a <c>GET</c> and changes no state.
/// </summary>
[Route("debug/render-error")]
public sealed class RenderErrorDemoPageController : Controller
{
    [HttpGet("")]
    public IActionResult Broken() => View("~/Views/Portal/Broken.cshtml");
}
