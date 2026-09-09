using System.Collections.Immutable;
using System.Text.Json;
using DomainCentric.BuildingBlocks.Ddd.Tactical;

namespace DcaShop.SharedKernel.Infrastructure.Events;

public enum PublicationStatus { Staged, Pending, Completed, Failed }

/// <summary>Retained progress for one consumer. Attempts counts failed deliveries; identity survives manual replay.</summary>
public sealed record ConsumerDelivery(string ConsumerId, PublicationStatus Status, int Attempts,
    string? LastError, DateTimeOffset? CompletedOn, DateTimeOffset? NextAttemptOn);

/// <summary>Snapshot payload and immutable per-consumer delivery state; the outbox owns transitions.</summary>
public sealed record IntegrationEventPublication(Guid Id, string Payload, Type EventType, DateTimeOffset RegisteredOn,
    PublicationStatus Status, int Attempts, string? LastError, DateTimeOffset? CompletedOn)
{
    public ImmutableDictionary<string, ConsumerDelivery> Consumers { get; init; } = ImmutableDictionary<string, ConsumerDelivery>.Empty;
    public bool ConsumersInitialized { get; init; }
    /// <summary>A fresh deserialization prevents a consumer or caller from changing the retained snapshot.</summary>
    public IIntegrationEvent Event => (IIntegrationEvent)(JsonSerializer.Deserialize(Payload, EventType)
        ?? throw new InvalidOperationException("An event snapshot must not deserialize to null"));
}

/// <summary>The provider key for a stable consumer/effect; local bookkeeping alone cannot prevent external duplicates.</summary>
public static class DeliveryIdentity
{
    public static string For(Guid eventId, string consumer, string effect = "default") => $"{eventId:D}:{consumer}:{effect}";
}
