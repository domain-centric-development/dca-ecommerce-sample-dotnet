using System.Text.Json;

using WireMock.RequestBuilders;
using WireMock.ResponseBuilders;
using WireMock.Server;

namespace DcaShop.E2eTests;

/// <summary>
/// The mail provider of the shops the suite starts: a stub on a free port, started with the shop, whose address the
/// shop is given. It accepts every mail and records it.
/// </summary>
public static class MailProviderStub
{
    private static readonly Lazy<WireMockServer> Server = new(Start, LazyThreadSafetyMode.ExecutionAndPublication);

    public static string Url => Server.Value.Url!;

    /// <summary>How long the shops of the suite wait for the stub — as long as for the payment provider, for the same reason.</summary>
    public const string Timeout = PaymentProviderStub.Timeout;

    /// <summary>From now on every mail is accepted; the mails so far are forgotten.</summary>
    public static void AcceptsMails()
    {
        Server.Value.Reset();
        Accept(Server.Value);
    }

    /// <summary>The mails to <paramref name="address"/> the provider has received within 15 seconds; at least one, if any arrives in time.</summary>
    public static async Task<IReadOnlyList<(string Template, string? OrderTotal, string? Currency)>> MailsWithinFifteenSecondsToAsync(string address)
    {
        var deadline = DateTime.UtcNow.AddSeconds(15);
        var mails = MailsTo(address);
        while (mails.Count == 0 && DateTime.UtcNow < deadline)
        {
            await Task.Delay(200);
            mails = MailsTo(address);
        }

        return mails;
    }

    private static List<(string Template, string? OrderTotal, string? Currency)> MailsTo(string address) =>
        Server.Value.LogEntries
            .Select(entry => entry.RequestMessage!)
            .Where(request => request.Method == "POST" && request.Path == "/messages")
            .Select(request =>
            {
                using var body = JsonDocument.Parse(request.Body!);
                var root = body.RootElement;
                var data = root.GetProperty("data");
                return (To: root.GetProperty("to").GetString(),
                    Template: root.GetProperty("template").GetString()!,
                    OrderTotal: data.TryGetProperty("orderTotal", out var total) ? total.GetString() : null,
                    Currency: data.TryGetProperty("currency", out var currency) ? currency.GetString() : null);
            })
            .Where(mail => mail.To == address)
            .Select(mail => (mail.Template, mail.OrderTotal, mail.Currency))
            .ToList();

    private static WireMockServer Start()
    {
        var server = WireMockServer.Start();
        Accept(server);
        AppDomain.CurrentDomain.ProcessExit += (_, _) => server.Stop();
        return server;
    }

    private static void Accept(WireMockServer server) =>
        server.Given(Request.Create().WithPath("/messages").UsingPost())
            .RespondWith(Response.Create().WithStatusCode(202));
}