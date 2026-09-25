using DcaShop.Account.Adapter.Outgoing.Security;
using DcaShop.Infrastructure;
using DcaShop.Web;

using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

var builder = WebApplication.CreateBuilder(args);

builder.Services
    .AddControllersWithViews(options =>
        // Every state-changing (non-GET) action must carry a valid antiforgery token -- except on the
        // token-only paths, which no cookie can authenticate, so there is no cross-site request to forge
        // (ADR-007). The exempt list is TokenOnlyPaths, the same one the authentication scheme is selected by.
        options.Filters.Add(new TokenOnlyAwareAntiforgeryFilter()))
    .AddApplicationPart(typeof(DcaShop.Account.AccountContext).Assembly)
    .AddApplicationPart(typeof(DcaShop.Backoffice.BackofficeContext).Assembly)
    .AddApplicationPart(typeof(DcaShop.Portal.PortalContext).Assembly)
    .AddApplicationPart(typeof(DcaShop.Product.ProductContext).Assembly)
    .AddApplicationPart(typeof(DcaShop.Cart.CartContext).Assembly)
    .AddApplicationPart(typeof(DcaShop.Checkout.CheckoutContext).Assembly);
builder.Services.AddDcaShop(builder.Configuration);

// RFC 9457 problem documents are the answer format of /api/** and /mcp. Each context registers one
// IExceptionHandler for its own routes; this is the service they write through.
builder.Services.AddProblemDetails();

// The antiforgery cookie follows the identity cookie's policy: a form inside a foreign frame sends its token
// only if the cookie carrying it may travel there too. Its default is SameSite=Strict, which the browser withholds
// exactly where the identity still arrives -- every POST would then fail as a missing token rather than a refused one.
var jwtSection = DcaShop.Account.Adapter.Outgoing.Security.JwtOptions.SectionName;
var crossSiteCookies = string.Equals(
    builder.Configuration[$"{jwtSection}:SameSite"],
    "None",
    StringComparison.OrdinalIgnoreCase);
var secureCookies = builder.Configuration.GetValue<bool>($"{jwtSection}:SecureCookies");
var allowFraming = builder.Configuration.GetValue<bool>($"{jwtSection}:AllowFraming");

builder.Services.AddAntiforgery(options =>
{
    options.Cookie.SameSite = crossSiteCookies ? SameSiteMode.None : SameSiteMode.Lax;

    // SameAsRequest rather than Always: ASP.NET Core refuses to issue a token at all when the cookie is Secure and
    // the request is not, which would turn a shop demoed over plain HTTP into a 500 on every page with a form. Over
    // HTTPS -- the only place a cross-site cookie works anyway -- this marks the cookie Secure just the same.
    options.Cookie.SecurePolicy = secureCookies ? CookieSecurePolicy.SameAsRequest : CookieSecurePolicy.None;

    // The framing decision is made once, by the middleware below, and for every response rather than only for the
    // pages that happen to render a token -- which is all ASP.NET Core's own header would cover.
    options.SuppressXFrameOptionsHeader = true;
});

var app = builder.Build();

if (app.Environment.IsDevelopment())
{
    app.UseDeveloperExceptionPage();
}
else
{
    app.UseExceptionHandler("/error");
    app.UseHsts();
    app.UseHttpsRedirection();
}

// The contexts' own exception handlers, for the token-only paths alone: /api/** and /mcp answer with a problem
// document, the pages keep the HTML error endpoint above. Registered after it so it is the inner handler and
// sees the failure first; a path that is not token-only never enters this branch.
app.UseWhen(
    context => TokenOnlyPaths.IsTokenOnlyEndpoint(context.Request.Path),
    api => api.UseExceptionHandler());

// Framing is refused unless the shop is configured as embeddable (Jwt:AllowFraming). Its own switch, because
// framing is about the origin -- where the port counts -- while the cookie policy is about the site, where it does
// not: a slide deck on another port needs framing allowed and nothing else. The Java sample does the same in
// SecurityConfiguration.
if (!allowFraming)
{
    app.Use(async (context, next) =>
    {
        context.Response.Headers["X-Frame-Options"] = "SAMEORIGIN";
        await next();
    });
}

app.UseStatusCodePagesWithReExecute("/error/{0}");
app.UseStaticFiles();

// Before any endpoint: the shop's default scheme puts the visitor identity on HttpContext.User — cookies on the
// pages, Bearer header on /api/** and /mcp — and the cart is keyed on it (ADR-008). The backoffice signs operators
// in under its own, explicitly named scheme; neither knows the other's cookies.
app.UseAuthentication();
app.UseAuthorization();

app.MapControllers();   // context controllers via AddApplicationPart, the error page from this assembly

// The Product Catalog's MCP tools. Bearer-only like /api/**: no cookie of this browser reaches it.
app.MapMcp("/mcp");

app.Run();

/// <summary>Entry point marker for <c>WebApplicationFactory</c>.</summary>
public partial class Program
{
}