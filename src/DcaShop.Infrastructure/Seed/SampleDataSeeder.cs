using DcaShop.Product.Application.CreateProduct;
using DcaShop.SharedKernel.Infrastructure.Events;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

namespace DcaShop.Infrastructure.Seed;

/// <summary>
/// Fills the in-memory catalog at start-up with what this architecture is made of: the books the guide grew
/// out of, modelling supplies for a design workshop, and hexagon merchandise. Only products are created here: the price and the stock of each
/// product are set by the Pricing and Inventory contexts when they receive <c>ProductCreatedEvent</c>. Delivery is
/// asynchronous, so the seeder waits until the outbox has no pending publication left — a start-up convenience, not
/// a pattern: nothing else in the shop waits for an integration event.
/// </summary>
public sealed class SampleDataSeeder : IHostedService
{
    private static readonly (string Sku, string Name, string Description, string ImageUrl, string Category, decimal Price, int Stock)[] Products =
    {
        // Books — the sources this architecture was synthesized from
        ("BOOK-001", "Domain-Driven Design", "The seminal work by Eric Evans that introduced the software industry to Domain-Driven Design. This essential guide teaches you how to tackle complexity in the heart of software by connecting implementation to an evolving model of the business domain.", "/images/products/ddd-book.webp", "Books", 54.99m, 20),
        ("BOOK-002", "Clean Architecture", "Robert C. Martin's definitive guide to software structure and design. Learn the universal rules of software architecture that dramatically improve developer productivity throughout the life of any software system.", "/images/products/clean-architecture-book.webp", "Books", 39.99m, 35),
        ("BOOK-003", "Implementing Domain-Driven Design", "Vaughn Vernon's hands-on companion to Evans: how aggregates, domain events and bounded contexts actually get built. The chapters on aggregate design rules and event-driven integration are the backbone of every context in this shop.", "/images/products/iddd-book.webp", "Books", 49.99m, 18),
        ("BOOK-004", "Domain-Driven Design Distilled", "The short version of Vernon's work — strategic design, context mapping and event storming in under 200 pages. The book to hand a colleague who has two hours rather than two months.", "/images/products/ddd-distilled-book.webp", "Books", 24.99m, 42),
        ("BOOK-005", "Hexagonal Architecture Explained", "Alistair Cockburn, with Juan Manuel Garrido de Paz, finally wrote the book on the pattern he named in 2005. Ports, adapters and the configurable dependency straight from the source, with two decades of misreadings corrected.", "/images/products/hexagonal-architecture-book.webp", "Books", 34.99m, 26),
        ("BOOK-006", "Learning Domain-Driven Design", "Vlad Khononov connects strategic design to architectural style: which subdomain deserves a rich domain model, and which one is perfectly well served by a transaction script. The reasoning behind pattern selection per subdomain.", "/images/products/learning-ddd-book.webp", "Books", 44.99m, 22),
        ("BOOK-007", "Patterns of Enterprise Application Architecture", "Martin Fowler's catalogue of the patterns every layered system rediscovers sooner or later — Repository, Unit of Work, Data Mapper, Service Layer. Twenty years on, still the reference for what a name in your adapter layer actually promises.", "/images/products/poeaa-book.webp", "Books", 59.99m, 14),
        ("BOOK-008", "Team Topologies", "Matthew Skelton and Manuel Pais on the other half of the boundary problem: a bounded context that no single team owns will not stay bounded. Stream-aligned teams, platform teams, and the inverse Conway manoeuvre.", "/images/products/team-topologies-book.webp", "Books", 29.99m, 30),
        // Modeling — everything a design workshop runs on
        ("STICKY-001", "Event Storming Sticky Note Kit", "Everything a modelling workshop needs, in the canonical colours: orange for domain events, blue for commands, yellow for aggregates, lilac for policies, pink for external systems. 500 sheets with extra-strong adhesive, plus a printed legend so nobody has to ask what the pink ones mean.", "/images/products/event-storming-stickies.webp", "Modeling", 34.99m, 40),
        ("MAGNET-001", "Hexagon Whiteboard Magnets, Set of 24", "Twenty-four laser-cut hexagon magnets in six colours, dry-erase on the face. Rearrange your bounded contexts until the context map stops looking like a plate of spaghetti — then wipe them clean and do it again tomorrow.", "/images/products/hexagon-magnets.webp", "Modeling", 24.99m, 35),
        ("POSTER-001", "Context Map Poster, A1", "All the strategic relationship patterns on one wall: Shared Kernel, Customer/Supplier, Conformist, Anti-Corruption Layer, Open Host Service, Published Language, Separate Ways, Partnership — and the Big Ball of Mud, so everyone can see where they are. Matte 200 g/m², ships rolled in a tube.", "/images/products/context-map-poster.webp", "Modeling", 19.99m, 60),
        ("CARDS-001", "DDD Pattern Card Deck", "Fifty-four cards, one pattern each: intent on the front, forces and traps on the back. Deal them out at the start of a design session so the team argues with the card instead of with each other.", "/images/products/pattern-card-deck.webp", "Modeling", 22.99m, 45),
        // Apparel
        ("SHIRT-001", "\"Ports & Adapters\" T-Shirt", "Heavyweight organic cotton with a screen-printed hexagon: incoming port on one edge, outgoing port on the other, domain in the middle. Explains your architecture before you have opened your laptop.", "/images/products/ports-adapters-tshirt.webp", "Apparel", 29.99m, 80),
        ("HOODIE-001", "\"Domain over Framework\" Hoodie", "Brushed-fleece hoodie in midnight navy, the slogan across the chest and the dependency arrow pointing inward on the sleeve. Warm enough for a data centre, quiet enough for a customer workshop.", "/images/products/domain-hoodie.webp", "Apparel", 59.99m, 40),
        ("CAP-001", "Hexagon Embroidered Cap", "Six-panel cotton twill with a golden hexagon embroidered on the front. Structured crown, curved brim, adjustable strap — the pattern on your head instead of on your slides.", "/images/products/hexagon-cap.webp", "Apparel", 24.99m, 50),
        // Desk & Office
        ("MUG-001", "\"Ubiquitous Language\" Mug", "350 ml of stoneware making a single point: one term, one meaning, everyone at the table. Dishwasher-safe and insulated well enough to survive a two-hour glossary discussion.", "/images/products/ubiquitous-language-mug.webp", "Desk & Office", 16.99m, 90),
        ("COASTER-001", "Hexagon Wooden Coasters, Set of 6", "Six oiled-oak hexagons, laser-engraved with concentric boundaries. The aggregates keep their invariants and your desk keeps its finish.", "/images/products/hexagon-coasters.webp", "Desk & Office", 27.99m, 30),
        ("NOTEBOOK-001", "Hex-Grid Modeling Notebook", "A5 hardcover with 192 pages of hexagonal grid instead of squares, so contexts, aggregates and their neighbours almost sketch themselves. Lay-flat binding, ribbon marker, elastic band.", "/images/products/hex-notebook.webp", "Desk & Office", 18.99m, 65),
        ("HEXAGON-001", "Wooden Hexagon Desk Model", "A solid beech hexagon on a walnut base, 12 cm across, with the domain engraved at the centre and three port notches cut into the edges. The hexagon you can actually buy — hand-finished in small batches, which is why there are never many in stock.", "/images/products/wooden-hexagon.webp", "Desk & Office", 39.99m, 8),
        // Stickers & Pins
        ("STICKER-001", "Hexagon Sticker Sheet", "Six die-cut vinyl hexagons, weatherproof and residue-free: ports, adapters, aggregate, domain event, context boundary, and one left blank for the purists. Laptop lid, water bottle, or the frame of the whiteboard.", "/images/products/hexagon-stickers.webp", "Stickers & Pins", 9.99m, 150),
        ("PIN-001", "\"Bounded Context\" Enamel Pin", "Hard-enamel pin, 25 mm, a gold-plated boundary around a deep magenta context with three ports on its edge. Butterfly clutch, backing card with a one-paragraph definition for the colleague who asks.", "/images/products/bounded-context-pin.webp", "Stickers & Pins", 12.99m, 75),
    };

    private static readonly TimeSpan DrainTimeout = TimeSpan.FromSeconds(30);

    private readonly IServiceScopeFactory _scopes;
    private readonly IIntegrationEventOutbox _outbox;

    public SampleDataSeeder(IServiceScopeFactory scopes, IIntegrationEventOutbox outbox)
    {
        _scopes = scopes;
        _outbox = outbox;
    }

    public async Task StartAsync(CancellationToken cancellationToken)
    {
        using var scope = _scopes.CreateScope();
        var createProduct = scope.ServiceProvider.GetRequiredService<ICreateProductInputPort>();

        foreach (var p in Products)
        {
            await createProduct.ExecuteAsync(
                new CreateProductCommand(p.Sku, p.Name, p.Description, p.ImageUrl, p.Price, "EUR", p.Category, p.Stock),
                cancellationToken).ConfigureAwait(false);
        }

        await WaitForOutboxAsync(cancellationToken).ConfigureAwait(false);
    }

    /// <summary>Waits until every publication the seeding caused has been delivered (or given up on).</summary>
    private async Task WaitForOutboxAsync(CancellationToken cancellationToken)
    {
        var deadline = DateTimeOffset.UtcNow + DrainTimeout;
        while (_outbox.All().Any(publication => publication.Status == PublicationStatus.Pending))
        {
            if (DateTimeOffset.UtcNow > deadline)
            {
                throw new TimeoutException("Sample data seeding timed out waiting for the integration-event outbox to drain.");
            }

            await Task.Delay(25, cancellationToken).ConfigureAwait(false);
        }
    }

    public Task StopAsync(CancellationToken cancellationToken) => Task.CompletedTask;

}
