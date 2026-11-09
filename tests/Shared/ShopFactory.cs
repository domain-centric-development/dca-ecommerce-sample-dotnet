using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;

namespace DcaShop.TestSupport;

/// <summary>
/// The shop as a test starts it: <see cref="WebApplicationFactory{TEntryPoint}"/> on a database of its own
/// (<see cref="TestDatabase"/>). Every host it builds gets a fresh one — a shop derived with
/// <c>WithWebHostBuilder</c> too — so a test that starts a shop of its own starts with the seeded catalogue and
/// nothing else, whatever the other tests of its class did.
/// </summary>
public class ShopFactory : WebApplicationFactory<Program>
{
    protected override void ConfigureWebHost(IWebHostBuilder builder) =>
        builder.UseSetting("ConnectionStrings:Shop", TestDatabase.Create());
}