using Microsoft.AspNetCore.Hosting.Server;
using Microsoft.AspNetCore.Hosting.Server.Features;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.DependencyInjection;

namespace DcaShop.E2eTests;

/// <summary>
/// The shop the browser suite drives: started once per test run, in this process, on a free port. With
/// <c>E2E_BASE_URL</c> the suite drives a shop started elsewhere instead; without it, nothing has to run
/// beforehand and every dependency the shop needs starts with it.
/// </summary>
public static class ShopUnderTest
{
    private static readonly Lazy<string> Url = new(Start, LazyThreadSafetyMode.ExecutionAndPublication);

    public static string BaseUrl => Url.Value;

    /// <summary>Whether the suite drives a shop started elsewhere, named by <c>E2E_BASE_URL</c>.</summary>
    public static bool StartedElsewhere => !string.IsNullOrWhiteSpace(Environment.GetEnvironmentVariable("E2E_BASE_URL"));

    private static string Start()
    {
        if (StartedElsewhere)
        {
            return Environment.GetEnvironmentVariable("E2E_BASE_URL")!.TrimEnd('/');
        }

        var factory = new WebApplicationFactory<Program>();
        factory.UseKestrel(0);
        factory.StartServer();
        AppDomain.CurrentDomain.ProcessExit += (_, _) => factory.Dispose();

        var address = factory.Services.GetRequiredService<IServer>().Features.Get<IServerAddressesFeature>()!.Addresses.First();
        // the port Kestrel chose, on the host name the embedded case pairs with 127.0.0.1
        return $"http://localhost:{new Uri(address).Port}";
    }
}