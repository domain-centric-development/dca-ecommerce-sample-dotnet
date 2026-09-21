using DomainCentric.BuildingBlocks.Ddd.Tactical;

namespace DcaShop.SharedKernel.Domain.Model;

/// <summary>Raised when two amounts in different currencies would be added, subtracted or compared.</summary>
/// <remarks>
/// Money is an amount <em>in</em> a currency; two of them in different currencies have no sum and no order
/// without a conversion rate, which is a decision nobody in this model is allowed to make silently. The rule
/// belongs to the shared kernel because every context that handles money holds it.
/// </remarks>
public sealed class CurrencyMismatchException : DomainException
{
    public CurrencyMismatchException(string operation, string left, string right)
        : base($"Cannot {operation} money in {left} and {right}")
    {
        Left = left;
        Right = right;
    }

    /// <summary>The ISO 4217 code of the amount the operation started from.</summary>
    public string Left { get; }

    /// <summary>The ISO 4217 code of the amount it was combined with.</summary>
    public string Right { get; }
}
