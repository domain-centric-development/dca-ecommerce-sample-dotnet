namespace DcaShop.Checkout.Domain.Model;

/// <summary>Step in the checkout flow, in order.</summary>
public enum CheckoutStep
{
    BuyerInfo = 1,
    Delivery = 2,
    Payment = 3,
    Review = 4,
    Confirmation = 5,
}

public static class CheckoutStepExtensions
{
    public static bool IsBefore(this CheckoutStep step, CheckoutStep other) => step < other;

    public static bool IsAfter(this CheckoutStep step, CheckoutStep other) => step > other;

    public static bool IsTerminal(this CheckoutStep step) => step == CheckoutStep.Confirmation;

    public static CheckoutStep Next(this CheckoutStep step) =>
        step.IsTerminal() ? throw new InvalidOperationException($"Cannot advance from terminal step: {step}") : step + 1;

    public static CheckoutStep Previous(this CheckoutStep step) =>
        step == CheckoutStep.BuyerInfo ? throw new InvalidOperationException($"Cannot go back from first step: {step}") : step - 1;
}
