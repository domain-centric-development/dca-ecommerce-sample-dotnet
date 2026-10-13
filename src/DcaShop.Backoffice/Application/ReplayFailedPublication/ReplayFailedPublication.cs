using DomainCentric.BuildingBlocks.Hexagonal.Ports.In;
using DomainCentric.BuildingBlocks.Hexagonal.Ports.Out;
namespace DcaShop.Backoffice.Application.ReplayFailedPublication;

public sealed record ReplayFailedPublicationCommand(Guid PublicationId, string ListenerId);
public sealed record ReplayFailedPublicationResult(bool Found);
public interface IReplayFailedPublicationInputPort : IUseCase<ReplayFailedPublicationCommand, ReplayFailedPublicationResult>;
public interface IEventPublicationRecoveryPort : IOutputPort
{
    Task<bool> ReplayFailedAsync(Guid publicationId, string listenerId, CancellationToken cancellationToken = default);
}
public sealed class ReplayFailedPublicationUseCase(IEventPublicationRecoveryPort recovery) : IReplayFailedPublicationInputPort
{
    public async Task<ReplayFailedPublicationResult> ExecuteAsync(ReplayFailedPublicationCommand command, CancellationToken cancellationToken = default) =>
        new(await recovery.ReplayFailedAsync(command.PublicationId, command.ListenerId, cancellationToken).ConfigureAwait(false));
}