using DcaShop.Backoffice.Application.Shared;

namespace DcaShop.Backoffice.Application.GetEventPublications;

/// <summary>Reads the publication log. A pure read: no transaction, nothing to change.</summary>
public sealed class GetEventPublicationsUseCase : IGetEventPublicationsInputPort
{
    private readonly IEventPublicationLogStore _log;

    public GetEventPublicationsUseCase(IEventPublicationLogStore log) => _log = log;

    public async Task<GetEventPublicationsResult> ExecuteAsync(
        GetEventPublicationsQuery query, CancellationToken cancellationToken = default) =>
        GetEventPublicationsResult.From(await _log.FindAllAsync(cancellationToken).ConfigureAwait(false));
}
