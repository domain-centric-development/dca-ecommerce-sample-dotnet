namespace DcaShop.Infrastructure.Events;

/// <summary>How often and how patiently the dispatcher retries a failing publication.</summary>
/// <param name="MaxAttempts">Total delivery attempts before the publication is marked failed.</param>
/// <param name="BaseDelay">Delay before the second attempt; doubles with every further attempt.</param>
public sealed record IntegrationEventRetryPolicy(int MaxAttempts, TimeSpan BaseDelay)
{
    public static IntegrationEventRetryPolicy Default { get; } = new(5, TimeSpan.FromMilliseconds(200));

    public TimeSpan DelayBefore(int nextAttempt) => BaseDelay * Math.Pow(2, Math.Max(0, nextAttempt - 2));
}
