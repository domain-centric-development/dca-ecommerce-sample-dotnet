using System.Globalization;
using System.Text.Json;

using WireMock.RequestBuilders;
using WireMock.ResponseBuilders;
using WireMock.Server;

namespace DcaShop.E2eTests;

/// <summary>
/// The payment provider of the shop the suite starts: a stub on a free port, started with the shop, whose address the
/// shop is given. Unless a test arranges otherwise it authorizes every payment, so every checkout of the suite can pay.
/// </summary>
public static class PaymentProviderStub
{
    private static readonly Lazy<WireMockServer> Server = new(Start, LazyThreadSafetyMode.ExecutionAndPublication);

    public static string Url => Server.Value.Url!;

    /// <summary>From now on every payment request is authorized with a payment reference; the requests so far are forgotten.</summary>
    public static void AuthorizesPayments()
    {
        Server.Value.Reset();
        Authorize(Server.Value);
    }

    /// <summary>The payment requests the provider received, as amount and currency.</summary>
    public static IReadOnlyList<(decimal Amount, string Currency)> PaymentRequests() =>
        Server.Value.LogEntries
            .Select(entry => entry.RequestMessage!)
            .Where(request => request.Method == "POST" && request.Path == "/payments")
            .Select(request =>
            {
                using var body = JsonDocument.Parse(request.Body!);
                return (body.RootElement.GetProperty("amount").GetDecimal(), body.RootElement.GetProperty("currency").GetString()!);
            })
            .ToList();

    /// <summary>A shown amount such as <c>9.99 EUR</c>, as amount and currency.</summary>
    public static (decimal Amount, string Currency) ParseShownAmount(string shown)
    {
        var parts = shown.Trim().Split(' ', StringSplitOptions.RemoveEmptyEntries);
        return (decimal.Parse(parts[0], CultureInfo.InvariantCulture), parts[1]);
    }

    private static WireMockServer Start()
    {
        var server = WireMockServer.Start();
        Authorize(server);
        AppDomain.CurrentDomain.ProcessExit += (_, _) => server.Stop();
        return server;
    }

    private static void Authorize(WireMockServer server) =>
        server.Given(Request.Create().WithPath("/payments").UsingPost())
            .RespondWith(Response.Create().WithStatusCode(201).WithBodyAsJson(new { reference = "e2e-" + Guid.NewGuid() }));
}