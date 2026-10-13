using DcaShop.Backoffice.Application.GetEventPublications;

using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.DependencyInjection;

namespace DcaShop.IntegrationTests;

/// <summary>
/// The event log through its input port, over the outbox the seeded shop filled at start-up: the order in which the
/// operator reads the entries.
/// </summary>
public sealed class EventLogOrderTest : IClassFixture<WebApplicationFactory<Program>>
{
    private readonly WebApplicationFactory<Program> _factory;

    public EventLogOrderTest(WebApplicationFactory<Program> factory)
    {
        _factory = factory;
    }

    [Fact]
    public async Task TheNewestEntryComesFirst()
    {
        using var scope = _factory.Services.CreateScope();
        var log = scope.ServiceProvider.GetRequiredService<IGetEventPublicationsInputPort>();

        var result = await log.ExecuteAsync(new GetEventPublicationsQuery());

        var published = result.Entries.Select(entry => entry.PublicationDate).ToList();
        Assert.True(published.Count > 1, "the seeded shop publishes several events at start-up");
        Assert.Equal(published.OrderByDescending(date => date), published);
        Assert.True(published[0] > published[^1], "the start-up events are published at different times");
    }
}