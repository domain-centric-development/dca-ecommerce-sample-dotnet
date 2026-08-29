using DcaShop.Infrastructure;

var builder = WebApplication.CreateBuilder(args);

builder.Services
    .AddControllersWithViews()
    .AddApplicationPart(typeof(DcaShop.Product.ProductContext).Assembly)
    .AddApplicationPart(typeof(DcaShop.Cart.CartContext).Assembly)
    .AddApplicationPart(typeof(DcaShop.Checkout.CheckoutContext).Assembly);
builder.Services.AddDcaShop();

var app = builder.Build();

app.UseStaticFiles();
app.MapGet("/", () => Results.Redirect("/products"));
app.MapControllers();

app.Run();

/// <summary>Entry point marker for <c>WebApplicationFactory</c>.</summary>
public partial class Program
{
}
