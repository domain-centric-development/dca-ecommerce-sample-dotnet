using DcaShop.Account.Infrastructure;
using DcaShop.Infrastructure;
using Microsoft.AspNetCore.Mvc;

var builder = WebApplication.CreateBuilder(args);

builder.Services
    .AddControllersWithViews(options =>
        // Every state-changing (non-GET) action must carry a valid antiforgery token.
        options.Filters.Add(new AutoValidateAntiforgeryTokenAttribute()))
    .AddApplicationPart(typeof(DcaShop.Account.AccountContext).Assembly)
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

app.MapControllers();   // context controllers via AddApplicationPart, the error page from this assembly

app.Run();

/// <summary>Entry point marker for <c>WebApplicationFactory</c>.</summary>
public partial class Program
{
}
