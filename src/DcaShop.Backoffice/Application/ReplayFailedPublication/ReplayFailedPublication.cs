using DomainCentric.BuildingBlocks.Application;
using DomainCentric.BuildingBlocks.Hexagonal.Ports.In;
using DomainCentric.BuildingBlocks.Hexagonal.Ports.Out;

using OneOf;

namespace DcaShop.Backoffice.Application.ReplayFailedPublication;

public sealed record ReplayFailedPublicationCommand(Guid PublicationId, string ListenerId);
/// <summary>What the retry came to: the delivery was handed back to the dispatcher, or there is no failed delivery to retry.</summary>
[GenerateOneOf]
public sealed partial class ReplayFailedPublicationResult
    : OneOfBase<ReplayFailedPublicationResult.Replayed, ReplayFailedPublicationResult.PublicationNotFound>
{
    /// <summary>The failed delivery was handed back to the dispatcher.</summary>
    public sealed record Replayed;

    /// <summary>No failed delivery of that publication to that listener.</summary>
    public sealed record PublicationNotFound(Guid PublicationId, string ListenerId) : IRefusal;
}
public interface IReplayFailedPublicationInputPort : IUseCase<ReplayFailedPublicationCommand, ReplayFailedPublicationResult>;
public interface IEventPublicationRecoveryPort : IOutputPort
{
    Task<bool> ReplayFailedAsync(Guid publicationId, string listenerId, CancellationToken cancellationToken = default);
}
public sealed class ReplayFailedPublicationUseCase(IEventPublicationRecoveryPort recovery) : IReplayFailedPublicationInputPort
{
    public async Task<ReplayFailedPublicationResult> ExecuteAsync(ReplayFailedPublicationCommand command, CancellationToken cancellationToken = default) =>
        await recovery.ReplayFailedAsync(command.PublicationId, command.ListenerId, cancellationToken).ConfigureAwait(false)
            ? new ReplayFailedPublicationResult.Replayed()
            : new ReplayFailedPublicationResult.PublicationNotFound(command.PublicationId, command.ListenerId);
}