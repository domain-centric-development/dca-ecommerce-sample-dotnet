using System.Text.Json;

using DcaShop.SharedKernel.Domain.Model;
using DcaShop.SharedKernel.Infrastructure.Events;

namespace DcaShop.UnitTests.SharedKernel;

/// <summary>A snapshot carries a currency as its ISO 4217 code and reads it back as the same currency.</summary>
public sealed class IntegrationEventJsonTest
{
    [Fact]
    public void MoneyTravelsWithItsCurrencyCode()
    {
        var json = JsonSerializer.Serialize(Money.Euro(4.99m), EventJson.Options);

        Assert.Equal("EUR", JsonDocument.Parse(json).RootElement.GetProperty("Currency").GetString());
        Assert.Equal(Money.Euro(4.99m), JsonSerializer.Deserialize<Money>(json, EventJson.Options));
    }
}