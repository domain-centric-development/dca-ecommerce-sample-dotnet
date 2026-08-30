namespace DcaShop.Backoffice;

/// <summary>
/// The Backoffice is an <b>operational module, not a bounded context</b>: it owns no business concept, only the
/// operator's view of how the shop is running. It therefore carries no <c>[BoundedContext]</c> marker and does
/// not appear in the context map — the same treatment as the infrastructure and web-host assemblies.
/// </summary>
/// <remarks>
/// Context-specific admin pages (editing a product, a price, a stock level) belong in their own bounded context
/// under <c>/backoffice/{context}/</c>, not here. This module holds what belongs to no context: today the
/// event-publication log, later dashboards and admin navigation.
/// </remarks>
public static class BackofficeModule
{
    /// <summary>Path prefix every backoffice page lives under, and the scope of its own authentication.</summary>
    public const string PathPrefix = "/backoffice";
}
