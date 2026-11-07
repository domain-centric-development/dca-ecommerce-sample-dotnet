using DcaShop.Checkout.Domain.Model;

namespace DcaShop.Checkout.Adapter.Incoming.Web.CheckoutCompletion;

/// <summary>
/// A refused step as the step pages answer it. Each page controller matches its use case's result, one function per
/// case, onto one of these; <see cref="CheckoutStepPage"/> then words it once for every step — which sentence, which
/// notice, or the step still open.
/// </summary>
/// <param name="What">What was refused.</param>
/// <param name="ProductName">The product a refusal names, where it names one.</param>
/// <param name="Count">The number of lines a refusal counts, where it counts them.</param>
internal sealed record StepRefusal(StepRefusal.Kind What, string? ProductName = null, int Count = 0)
{
    /// <summary>What a step's refusal is about, as far as the pages tell them apart.</summary>
    internal enum Kind
    {
        SessionNotFound,
        NotModifiable,
        StepNotCompleted,
        StepOutOfOrder,
        ShippingOptionNotOffered,
        PaymentProviderNotFound,
        PaymentProviderUnavailable,
        PaymentRefused,
        NothingToPay,
        StockRanShort,
        PricesChanged,
        PriceChangedSinceReview,
        OrderChangedSinceReview,

        /// <summary>A form field reached a value object that refused it — still thrown, not a case.</summary>
        ValueRefused,

        /// <summary>A rule of the model no use case returns as a case of its own — still thrown.</summary>
        RuleRefused,
    }

    public static StepRefusal SessionNotFound { get; } = new(Kind.SessionNotFound);

    public static StepRefusal NotModifiable { get; } = new(Kind.NotModifiable);

    public static StepRefusal StepNotCompleted { get; } = new(Kind.StepNotCompleted);

    public static StepRefusal StepOutOfOrder { get; } = new(Kind.StepOutOfOrder);

    public static StepRefusal ShippingOptionNotOffered { get; } = new(Kind.ShippingOptionNotOffered);

    public static StepRefusal PaymentProviderNotFound { get; } = new(Kind.PaymentProviderNotFound);

    public static StepRefusal PaymentProviderUnavailable { get; } = new(Kind.PaymentProviderUnavailable);

    public static StepRefusal PaymentRefused { get; } = new(Kind.PaymentRefused);

    public static StepRefusal NothingToPay { get; } = new(Kind.NothingToPay);

    public static StepRefusal OrderChangedSinceReview { get; } = new(Kind.OrderChangedSinceReview);

    public static StepRefusal ValueRefused { get; } = new(Kind.ValueRefused);

    public static StepRefusal RuleRefused { get; } = new(Kind.RuleRefused);

    /// <summary>A line's price changed since the review; the line is named by its product.</summary>
    public static StepRefusal PriceChangedSinceReview(string productName) => new(Kind.PriceChangedSinceReview, productName);

    /// <summary>The lines the facts of this moment refuse: a line short of stock is named, other changes are counted.</summary>
    public static StepRefusal Invalid(CheckoutValidationResult validation)
    {
        ArgumentNullException.ThrowIfNull(validation);
        return validation.ShortOfStock is { } shortLine
            ? new(Kind.StockRanShort, shortLine.ProductName)
            : new(Kind.PricesChanged, Count: validation.Errors.Count);
    }

    /// <summary>A step sent before the earlier steps are done.</summary>
    public bool SentTooEarly => What is Kind.StepNotCompleted or Kind.StepOutOfOrder;
}