using System.Text.Json;

using WireMock.RequestBuilders;
using WireMock.ResponseBuilders;
using WireMock.Server;

namespace DcaShop.IntegrationTests;

/// <summary>A mail request the mail provider stub received: its recipient, template, key, the order total it carries, and the status it was answered with.</summary>
internal sealed record ReceivedMail(string To, string Template, string Key, string? OrderTotal, string? Currency, int Status);

/// <summary>What the mail provider stub is told to answer, and the mail requests it received.</summary>
internal static class MailProviderStubs
{
    /// <summary>From now on every mail is accepted; what was arranged before is forgotten, what was received is kept.</summary>
    public static void AcceptsMails(this WireMockServer mailProvider)
    {
        mailProvider.ResetMappings();
        mailProvider.Given(Request.Create().WithPath("/messages").UsingPost())
            .RespondWith(Response.Create().WithStatusCode(202));
    }

    /// <summary>From now on every mail is answered as by a provider that is down.</summary>
    public static void IsUnavailable(this WireMockServer mailProvider)
    {
        mailProvider.ResetMappings();
        mailProvider.Given(Request.Create().WithPath("/messages").UsingPost())
            .RespondWith(Response.Create().WithStatusCode(503));
    }

    /// <summary>Every mail request to <paramref name="address"/> the provider received, in order.</summary>
    public static List<ReceivedMail> MailsTo(this WireMockServer mailProvider, string address) =>
        mailProvider.LogEntries
            .Where(entry => entry.RequestMessage!.Method == "POST" && entry.RequestMessage.Path == "/messages")
            .Select(entry =>
            {
                using var body = JsonDocument.Parse(entry.RequestMessage!.Body!);
                var root = body.RootElement;
                var data = root.TryGetProperty("data", out var values) ? values : default;
                return new ReceivedMail(
                    root.GetProperty("to").GetString()!,
                    root.GetProperty("template").GetString()!,
                    root.GetProperty("key").GetString()!,
                    Text(data, "orderTotal"),
                    Text(data, "currency"),
                    Convert.ToInt32(entry.ResponseMessage!.StatusCode, System.Globalization.CultureInfo.InvariantCulture));
            })
            .Where(mail => mail.To == address)
            .ToList();

    /// <summary>Reads <paramref name="read"/> until <paramref name="enough"/> holds or 15 seconds have passed, and answers the last reading.</summary>
    public static async Task<T> WithinFifteenSecondsAsync<T>(Func<T> read, Func<T, bool> enough)
    {
        var deadline = DateTime.UtcNow.AddSeconds(15);
        var reading = read();
        while (!enough(reading) && DateTime.UtcNow < deadline)
        {
            await Task.Delay(100);
            reading = read();
        }

        return reading;
    }

    private static string? Text(JsonElement data, string name) =>
        data.ValueKind == JsonValueKind.Object && data.TryGetProperty(name, out var value) ? value.GetString() : null;
}