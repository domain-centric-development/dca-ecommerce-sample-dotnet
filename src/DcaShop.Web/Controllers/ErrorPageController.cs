using Microsoft.AspNetCore.Mvc;

namespace DcaShop.Web.Controllers;

/// <summary>Status-code pages (re-executed by <c>UseStatusCodePagesWithReExecute</c>) and the production exception handler target.</summary>
public sealed class ErrorPageController : Controller
{
    [Route("/error/{code:int?}")]
    public IActionResult Show(int? code)
    {
        Response.StatusCode = code ?? 500;
        return code == 404 ? View("~/Views/Error/404.cshtml") : View("~/Views/Error/Error.cshtml", code ?? 500);
    }
}
