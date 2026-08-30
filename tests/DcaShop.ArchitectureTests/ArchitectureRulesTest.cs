using System.Reflection;
using DomainCentric.ArchRules;
using DomainCentric.ArchRules.Xunit;

namespace DcaShop.ArchitectureTests;

/// <summary>
/// The whole DCA rule catalog against every assembly of the shop — one theory case per rule, named by
/// its set (<c>tactical / DCA-TAC-001</c>).
/// </summary>
/// <remarks>
/// The reference implementation runs the catalog unabridged — every rule at
/// <see cref="DcaSeverity.Error"/>. A project adopting DCA on an existing code base rarely can, and
/// does not have to: <see cref="DcaRuleSelection"/> narrows the run, lowers a rule to a warning, or
/// tolerates a documented exception. Configure it in <c>dca-archunit.properties</c> next to the test
/// assembly, in <c>AdditionalSelection</c>, or both — the file is the base, the property is applied
/// on top. Only the file shapes the report (theory names and the test count), so a decision that
/// should be visible there belongs in it.
/// </remarks>
public sealed class ArchitectureRulesTest : DcaArchitectureTest
{
    protected override DcaLayout Layout => DcaLayout.ForRootNamespace("DcaShop");

    /// <summary>
    /// Applied on top of <c>dca-archunit.properties</c> — the reference implementation adds nothing.
    /// Everything a consuming project may need:
    /// <code>
    /// DcaRuleSelection.All()
    ///     .OnlySets("cycles", "layered", "hexagonal")            // adopt the catalog in stages
    ///     .Excluding("DCA-NAM-005", "no MVC controllers here")   // off, with the reason
    ///     .Warning("DCA-TAC-009", "being made sealed")           // reported, does not fail the build
    ///     .IgnoringViolationsMatching("DCA-STR-003", ".*Legacy.*");   // documented exception
    /// </code>
    /// </summary>
    protected override DcaRuleSelection AdditionalSelection => DcaRuleSelection.All();

    protected override IEnumerable<Assembly> Assemblies => new[]
    {
        typeof(SharedKernel.SharedKernelContext).Assembly,
        typeof(Pricing.PricingContext).Assembly,
        typeof(Inventory.InventoryContext).Assembly,
        typeof(Product.ProductContext).Assembly,
        typeof(Cart.CartContext).Assembly,
        typeof(Checkout.CheckoutContext).Assembly,
        typeof(Infrastructure.DcaShopRegistration).Assembly,
    };
}
