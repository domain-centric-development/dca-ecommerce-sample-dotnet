using DcaShop.Infrastructure;
using Microsoft.AspNetCore.Mvc;

var builder = WebApplication.CreateBuilder(args);

builder.Services
    .AddControllersWithViews(options =>
        // Every state-changing (non-GET) action must carry a valid antiforgery token.
        options.Filters.Add(new AutoValidateAntiforgeryTokenAttribute()))
    .AddApplicationPart(typeof(DcaShop.Product.ProductContext).Assembly)
    .AddApplicationPart(typeof(DcaShop.Cart.CartContext).Assembly)
    .AddApplicationPart(typeof(DcaShop.Checkout.CheckoutContext).Assembly);
builder.Services.AddDcaShop();

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
app.MapGet("/", () => Results.Redirect("/products"));
app.Map("/error/{code:int?}", (int? code) => Results.Problem(statusCode: code ?? 500));
app.MapControllers();

app.Run();

/// <summary>Entry point marker for <c>WebApplicationFactory</c>.</summary>
public partial class Program
{
}
