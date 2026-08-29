using System.Reflection;
using DomainCentric.ArchRules;
using DomainCentric.ArchRules.Xunit;

namespace DcaShop.ArchitectureTests;

/// <summary>The whole DCA rule catalog against every assembly of the shop — one theory case per rule.</summary>
public sealed class ArchitectureRulesTest : DcaArchitectureTest
{
    protected override DcaLayout Layout => DcaLayout.ForRootNamespace("DcaShop");

    protected override IEnumerable<Assembly> Assemblies => new[]
    {
        typeof(SharedKernel.SharedKernelContext).Assembly,
        typeof(Product.ProductContext).Assembly,
        typeof(Cart.CartContext).Assembly,
        typeof(Checkout.CheckoutContext).Assembly,
        typeof(Infrastructure.DcaShopRegistration).Assembly,
    };
}
