using System.Globalization;
using DomainCentric.BuildingBlocks.Ddd.Tactical;

namespace DcaShop.SharedKernel.Domain.Model;

/// <summary>A non-negative amount in one currency, normalised to two decimal places.</summary>
public sealed record Money : IValue
{
    public Money(decimal amount, string currency)
    {
        if (amount < 0)
        {
            throw new ArgumentException("Amount cannot be negative", nameof(amount));
        }

        if (string.IsNullOrWhiteSpace(currency) || currency.Length != 3)
        {
            throw new ArgumentException("Currency must be an ISO 4217 code", nameof(currency));
        }

        Amount = decimal.Round(amount, 2, MidpointRounding.AwayFromZero);
        Currency = currency.ToUpperInvariant();
    }

    public decimal Amount { get; }

    public string Currency { get; }

    public static Money Of(decimal amount, string currency) => new(amount, currency);

    public static Money Euro(decimal amount) => new(amount, "EUR");

    public static Money Zero(string currency) => new(0m, currency);

    public Money Add(Money other) => new(Amount + SameCurrency(other).Amount, Currency);

    public Money Subtract(Money other) => new(Amount - SameCurrency(other).Amount, Currency);

    public Money Multiply(int factor) => new(Amount * factor, Currency);

    public Money Multiply(decimal factor) => new(Amount * factor, Currency);

    public bool IsZero => Amount == 0m;

    public bool IsGreaterThan(Money other) => Amount > SameCurrency(other).Amount;

    public override string ToString() => Amount.ToString("0.00", CultureInfo.InvariantCulture) + " " + Currency;

    private Money SameCurrency(Money other)
    {
        if (other.Currency != Currency)
        {
            throw new ArgumentException($"Cannot combine {Currency} with {other.Currency}", nameof(other));
        }

        return other;
    }
}
