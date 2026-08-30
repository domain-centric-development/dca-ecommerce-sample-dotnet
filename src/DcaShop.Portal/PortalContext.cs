using DomainCentric.BuildingBlocks.Ddd.Strategic;

namespace DcaShop.Portal;

/// <summary>
/// Portal bounded context: the shop's user interface shell. It owns the landing page and the navigation, and it
/// displays concepts of other contexts without owning any of them — it has no aggregates, no value objects, no
/// events and no use cases of its own.
/// </summary>
/// <remarks>
/// It is marked as a bounded context because that is what the Java sample does, but functionally it is a generic
/// subdomain for UI composition; the glossary records that as an open question. It refers to other contexts only
/// by link, never by call, which is why it declares no upstream.
/// </remarks>
[BoundedContext("Portal", Description = "Web portal, user interface composition, and cross-context views")]
public static class PortalContext
{
}
