using DomainCentric.BuildingBlocks.Ddd.Strategic;
using DomainCentric.BuildingBlocks.Ddd.Strategic.Relationships;

namespace DcaShop.Pricing;

/// <summary>
/// Pricing bounded context: owns what a product costs and when that price took effect. It knows nothing about the
/// catalog — a price record is created when an event carrying <see cref="Events.IPriceInitializationTrigger"/>
/// arrives, and read back through the published <see cref="Api.PricingService"/>.
/// </summary>
[BoundedContext("Pricing", Description = "Product pricing management and price change tracking")]
[Partnership("Product",
    Rationale = "Pricing owns the consumer-defined IPriceInitializationTrigger contract that catalog events implement; both contexts evolve it together")]
public static class PricingContext
{
}
