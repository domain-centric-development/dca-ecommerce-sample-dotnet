using DomainCentric.BuildingBlocks.Ddd.Tactical;

namespace DcaShop.SharedKernel.Domain.Model;

/// <summary>A currency, named by its ISO 4217 code — the currency an amount of <see cref="Money"/> is in.</summary>
/// <remarks>
/// A code is a value from a fixed set, so it is a type rather than a string: only a code ISO 4217 knows can be
/// held, and two currencies are equal exactly when their codes are. The set is the one the Java twin's platform
/// type accepts (<see cref="IsoCurrencyCodes"/>). The code is what leaves the model — in a DTO, an integration
/// event or a message — and <see cref="ToString"/> prints it.
/// </remarks>
public sealed record Currency : IValue
{
    /// <summary>The euro, the currency the shop prices in.</summary>
    public static readonly Currency Euro = new("EUR");

    private Currency(string code) => Code = code;

    /// <summary>The ISO 4217 code, three upper-case letters.</summary>
    public string Code { get; }

    /// <summary>The currency an ISO 4217 code names.</summary>
    /// <exception cref="ArgumentException">The code is empty or names no ISO 4217 currency.</exception>
    public static Currency Of(string code)
    {
        if (string.IsNullOrWhiteSpace(code) || !IsoCurrencyCodes.Contains(code))
        {
            throw new ArgumentException("Currency must be an ISO 4217 code", "currency");
        }

        return new Currency(code);
    }

    public override string ToString() => Code;
}