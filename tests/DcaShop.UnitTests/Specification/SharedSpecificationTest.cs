using System.Text.Json;
using System.Globalization;
using DcaShop.Cart.Domain.Model;
using DcaShop.SharedKernel.Domain.Model;
namespace DcaShop.UnitTests.Specification;

public sealed class SharedSpecificationTest
{
    [SpecificationFact]
    public void EveryVectorFamilyHasAnAdapterAndExceptionsAreCurrent()
    {
        var root = Path.Combine(AppContext.BaseDirectory, "specification");
        Assert.Equal(new[] { "money.json", "quantity.json", "product.json", "cart-reconciliation.json", "checkout.json", "delivery.json" }.Order(), Directory.GetFiles(Path.Combine(root, "vectors"), "*.json").Select(Path.GetFileName).Order());
        var ids = new HashSet<string>();
        foreach (var path in Directory.GetFiles(Path.Combine(root, "vectors"), "*.json"))
        {
            using var document = JsonDocument.Parse(File.ReadAllText(path));
            foreach (var node in document.RootElement.EnumerateArray()) Assert.True(ids.Add(node.GetProperty("id").GetString()!), "Duplicate vector id");
        }
        foreach (var line in File.ReadAllLines(Path.Combine(root, "exceptions.md")))
        {
            if (!line.StartsWith('|')) continue; var cells = line.Split('|');
            if (cells.Length < 8 || cells[1].Trim() == "Id" || cells[1].Contains("---", StringComparison.Ordinal)) continue;
            Assert.True(DateOnly.Parse(cells[6].Trim(), CultureInfo.InvariantCulture) >= DateOnly.FromDateTime(DateTime.UtcNow), "Expired specification exception: " + cells[1]);
        }
    }

    public static IEnumerable<object[]> Vectors()
    {
        if (!SpecificationVectors.Present) yield break;
        foreach (var file in new[] { "money.json", "quantity.json", "product.json", "cart-reconciliation.json" })
        {
            using var document = JsonDocument.Parse(File.ReadAllText(Path.Combine(AppContext.BaseDirectory, "specification", "vectors", file)));
            foreach (var vector in document.RootElement.EnumerateArray()) yield return new object[] { file, vector.GetRawText() };
        }
    }
    [SpecificationTheory, MemberData(nameof(Vectors))]
    public void DrivesRealDomain(string file, string json)
    {
        using var document = JsonDocument.Parse(json); var v = document.RootElement;
        switch (file)
        {
            case "money.json":
                {
                    Action action = () =>
                    {
                        var result = Money.Of(decimal.Parse(v.GetProperty("amount").GetString()!, CultureInfo.InvariantCulture), v.GetProperty("currency").GetString()!);
                        if (v.TryGetProperty("subtract", out var subtract)) result = result.Subtract(Money.Of(decimal.Parse(subtract.GetString()!, CultureInfo.InvariantCulture), result.Currency));
                        if (v.GetProperty("accept").GetBoolean()) Assert.Equal(v.GetProperty("normalized").GetString(), result.Amount.ToString("0.00", CultureInfo.InvariantCulture));
                    };
                    if (v.GetProperty("accept").GetBoolean()) action(); else Assert.ThrowsAny<ArgumentException>(action);
                    break;
                }
            case "quantity.json":
                {
                    Assert.ThrowsAny<ArgumentException>(() => Quantity.Of(v.GetProperty("quantity").GetInt32()));
                    if (v.GetProperty("id").GetString() == "quantity.default-reconstitution")
                    {
                        var stored = new ShoppingCart.StoredItem(CartItemId.Generate(), ProductId.Generate(), default, Price.Of(Money.Euro(1)));
                        Assert.ThrowsAny<ArgumentException>(() => ShoppingCart.Reconstitute(CartId.Generate(), CustomerId.Of("specification"), CartStatus.Active, new[] { stored }));
                    }
                    break;
                }
            case "cart-reconciliation.json": Reconciliation(v); break;
            case "product.json": Product(v); break;
            default: Assert.Fail("Vector has no adapter: " + v.GetProperty("id")); break;
        }
    }
    private static void Reconciliation(JsonElement v)
    {
        var cart = new ShoppingCart(CartId.Generate(), CustomerId.Of("specification")); var product = ProductId.Generate(); var price = Price.Of(Money.Euro(10));
        cart.AddItem(product, Quantity.Of(v.GetProperty("initial").GetInt32()), price); var snapshot = cart.Items[0].PositionSnapshot;
        foreach (var edit in v.GetProperty("edits").EnumerateArray())
        {
            if (edit.TryGetProperty("add", out var add)) cart.AddItem(product, Quantity.Of(add.GetInt32()), price);
            else if (edit.TryGetProperty("set", out var quantity)) cart.UpdateItemQuantity(cart.Items[0].Id, Quantity.Of(quantity.GetInt32()));
            else if (edit.TryGetProperty("remove", out _)) cart.RemoveItemByProductId(product);
            else if (edit.TryGetProperty("other", out var other)) cart.AddItem(ProductId.Generate(), Quantity.Of(other.GetInt32()), price);
            else Assert.Fail("Unknown edit " + edit);
        }
        var later = cart.Items.Select(i => i.PositionSnapshot).ToArray();
        cart = ShoppingCart.Reconstitute(cart.Id, cart.CustomerId, cart.Status, cart.Items.Select(i => new ShoppingCart.StoredItem(i.Id, i.ProductId, i.Quantity, i.PriceAtAddition, i.StoredUnits)));
        cart.ReconcileCheckout("session-1", new[] { snapshot });
        if (v.TryGetProperty("overlap", out var overlap) && overlap.GetBoolean()) cart.ReconcileCheckout("session-2", later);
        Assert.Equal(v.GetProperty("remaining").GetInt32(), cart.TotalQuantity); Assert.True(cart.IsActive);
        cart.ClearDomainEvents(); cart.ReconcileCheckout("session-1", new[] { snapshot });
        Assert.Equal(v.GetProperty("remaining").GetInt32(), cart.TotalQuantity); Assert.Empty(cart.DomainEvents);
    }
    private static void Product(JsonElement v)
    {
        Action action = () =>
        {
            var product = new DcaShop.Product.Domain.Model.ProductFactory().Create(DcaShop.Product.Domain.Model.Sku.Of("SPEC-1"), DcaShop.Product.Domain.Model.ProductName.Of("Specification product"), DcaShop.Product.Domain.Model.ProductDescription.Empty(), DcaShop.Product.Domain.Model.Category.Of("Test"), DcaShop.Product.Domain.Model.ImageUrl.None(), Price.Of(Money.Euro(1)), v.GetProperty("stock").GetInt32());
            Assert.Single(product.DomainEvents);
            if (v.TryGetProperty("fields", out var fields))
            {
                var outbox = new DcaShop.SharedKernel.Infrastructure.Events.InMemoryIntegrationEventOutbox(TimeProvider.System);
                var publisher = new DcaShop.SharedKernel.Adapter.Outgoing.Event.OutboxIntegrationEventPublisher(outbox, new DcaShop.SharedKernel.Infrastructure.Transactions.InMemoryTransactionBoundary());
                new DcaShop.Product.Adapter.Outgoing.Event.ProductCreatedEventPublisher(publisher).OnAsync((object)product.DomainEvents.Single()).GetAwaiter().GetResult();
                using var payload = JsonDocument.Parse(outbox.All().Single().Payload);
                Assert.Equal(fields.EnumerateArray().Select(f => f.GetString()).Order(), payload.RootElement.EnumerateObject().Select(p => p.Name).Order());
                Assert.Equal("1.00", payload.RootElement.GetProperty("amount").GetString());
                Assert.Equal("EUR", payload.RootElement.GetProperty("currency").GetString());
                Assert.Equal(v.GetProperty("stock").GetInt32(), payload.RootElement.GetProperty("initialStock").GetInt32());
            }
        };
        if (v.GetProperty("accept").GetBoolean()) action(); else Assert.ThrowsAny<ArgumentException>(action);
    }
}
