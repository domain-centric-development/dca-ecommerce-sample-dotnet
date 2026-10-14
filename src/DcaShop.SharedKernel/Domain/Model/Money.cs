using System.Globalization;

using DomainCentric.BuildingBlocks.Ddd.Tactical;

namespace DcaShop.SharedKernel.Domain.Model;

/// <summary>A non-negative amount in one currency, normalised to two decimal places.</summary>
public sealed record Money : IValue
{
    public Money(decimal amount, Currency currency)
    {
        ArgumentNullException.ThrowIfNull(currency);
        if (amount < 0)
        {
            throw new ArgumentException("Amount cannot be negative", nameof(amount));
        }

        if (amount > 999999999999.99m) throw new ArgumentException("Amount exceeds maximum monetary value", nameof(amount));

        Amount = decimal.Round(amount, 2, MidpointRounding.AwayFromZero);
        Currency = currency;
    }

    public decimal Amount { get; }

    /// <summary>Whether there is an amount at all. An amount is never negative, so this is "not zero".</summary>
    public bool IsPositive => Amount != 0m;

    public Currency Currency { get; }

    public static Money Of(decimal amount, Currency currency) => new(amount, currency);

    public static Money Euro(decimal amount) => new(amount, Currency.Euro);

    public static Money Zero(Currency currency) => new(0m, currency);

    public Money Add(Money other) => new(Amount + SameCurrency(other, "add").Amount, Currency);

    public Money Subtract(Money other) => new(Amount - SameCurrency(other, "subtract").Amount, Currency);

    public Money Multiply(int factor) => new(Amount * factor, Currency);

    public Money Multiply(decimal factor) => new(Amount * factor, Currency);

    public bool IsZero => Amount == 0m;

    public bool IsGreaterThan(Money other) => Amount > SameCurrency(other, "compare").Amount;

    public override string ToString() => Amount.ToString("0.00", CultureInfo.InvariantCulture) + " " + Currency.Code;

    private Money SameCurrency(Money other, string operation)
    {
        if (other.Currency != Currency)
        {
            throw new CurrencyMismatchException(operation, Currency, other.Currency);
        }

        return other;
    }
}