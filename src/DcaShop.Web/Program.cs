using DcaShop.Account.Infrastructure;
using DcaShop.Infrastructure;
using DcaShop.Web;
using Microsoft.AspNetCore.Mvc;

var builder = WebApplication.CreateBuilder(args);

builder.Services
    .AddControllersWithViews(options =>
        // Every state-changing (non-GET) action must carry a valid antiforgery token -- except on the
        // token-only paths, which no cookie can authenticate, so there is no cross-site request to forge
        // (ADR-007). The exempt list is JwtAuthenticationMiddleware's own; the two must never drift apart.
        options.Filters.Add(new TokenOnlyAwareAntiforgeryFilter()))
    .AddApplicationPart(typeof(DcaShop.Account.AccountContext).Assembly)
    .AddApplicationPart(typeof(DcaShop.Backoffice.BackofficeModule).Assembly)
    .AddApplicationPart(typeof(DcaShop.Portal.PortalContext).Assembly)
    .AddApplicationPart(typeof(DcaShop.Product.ProductContext).Assembly)
    .AddApplicationPart(typeof(DcaShop.Cart.CartContext).Assembly)
    .AddApplicationPart(typeof(DcaShop.Checkout.CheckoutContext).Assembly);
builder.Services.AddDcaShop(builder.Configuration);

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

app.UseStatusCodePagesWithReExecute("/error/{0}");
app.UseStaticFiles();

// Before any endpoint: every page reads the visitor identity, and the cart is keyed on it.
app.UseDcaShopIdentity();

// The backoffice signs operators in under its own scheme; the shop's identity middleware above does not know
// about it, and it does not know about the shop's cookies.
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
