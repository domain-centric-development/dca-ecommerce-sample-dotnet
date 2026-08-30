using DomainCentric.BuildingBlocks.Ddd.Tactical;

namespace DcaShop.SharedKernel.Infrastructure.Events;

/// <summary>Lifecycle of a registered integration event publication.</summary>
public enum PublicationStatus
{
    /// <summary>Registered, not yet successfully delivered to every listener.</summary>
    Pending,

    /// <summary>Every listener handled the event.</summary>
    Completed,

    /// <summary>Delivery gave up after the retry policy was exhausted; kept for inspection.</summary>
    Failed,
}

/// <summary>
/// One integration event on its way to the consumers: the event itself plus delivery bookkeeping.
/// The outbox owns the state; the dispatcher advances it.
/// </summary>
public sealed record IntegrationEventPublication(
    Guid Id,
    IIntegrationEvent Event,
    DateTimeOffset RegisteredOn,
    PublicationStatus Status,
    int Attempts,
    string? LastError,
    DateTimeOffset? CompletedOn);
