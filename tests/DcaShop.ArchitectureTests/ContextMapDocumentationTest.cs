using DomainCentric.ArchRules;
using DomainCentric.ArchRules.ContextMap;

namespace DcaShop.ArchitectureTests;

/// <summary>Renders the executable context map to <c>docs/context-map.md</c> so the document can never drift from the code.</summary>
public sealed class ContextMapDocumentationTest
{
    [Fact]
    public void RendersContextMap()
    {
        var layout = DcaLayout.ForRootNamespace("DcaShop");
        var arch = DcaArchitecture.Load(
            layout,
            typeof(SharedKernel.SharedKernelContext).Assembly,
            typeof(Account.AccountContext).Assembly,
            typeof(Backoffice.BackofficeModule).Assembly,
            typeof(Portal.PortalContext).Assembly,
            typeof(Pricing.PricingContext).Assembly,
            typeof(Inventory.InventoryContext).Assembly,
            typeof(Product.ProductContext).Assembly,
            typeof(Cart.CartContext).Assembly,
            typeof(Checkout.CheckoutContext).Assembly);

        var markdown = ContextMapRenderer.Of(arch).WithTitle("DcaShop Context Map").Render();
        Assert.Contains("Shopping Cart", markdown, StringComparison.Ordinal);

        var target = FindRepositoryRoot();
        if (target is not null)
        {
            File.WriteAllText(Path.Combine(target, "docs", "context-map.md"), markdown);
        }
    }

    private static string? FindRepositoryRoot()
    {
        for (var dir = new DirectoryInfo(AppContext.BaseDirectory); dir is not null; dir = dir.Parent)
        {
            if (File.Exists(Path.Combine(dir.FullName, "DcaShop.sln")))
            {
                return dir.FullName;
            }
        }

        return null;
    }
}
