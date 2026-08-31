# ADR-003: Pattern Selection per Context and Stage-1 Stand-ins

**Date**: 2026-08-30 · **Status**: Accepted

## Context

DCA's full tactical pattern set pays off in core subdomains and over-engineers simple ones. This sample exists to
*demonstrate* the patterns, and it is built in stages: stage 1 has Product Catalog, Shopping Cart and Checkout;
Pricing, Inventory, Account, Portal and Backoffice come later.

## Decision

1. **All three stage-1 contexts are core subdomains and use the rich domain-model style** with the full rule
   set: aggregates with invariants and domain events, value objects, ports and adapters, ACLs at every
   cross-context edge. Later supporting contexts may choose transaction-script style with the structural rules
   only; each context records its choice here when it arrives.
2. **Stand-ins for missing upstreams keep the ports honest.** *(superseded 2026-08-30 by WP-12: the Pricing and
   Inventory contexts exist; both stand-in adapters are deleted and `PricingDataAdapter` /
   `InventoryStockDataAdapter` call the real Open Host Services. Completed 2026-08-31: price and stock left the
   catalog's Api as this paragraph foresaw — Cart and Checkout declare `[Upstream("Pricing")]` and
   `[Upstream("Inventory")]` and compose the three Open Host Services in `CompositeArticleDataAdapter` /
   `CompositeCheckoutArticleDataAdapter`, the same shape as the Java sample. The paragraph stays as the record of
   how the stage-1 ports were kept honest.)* The Product Catalog already depends on
   `IPricingDataPort` and `IProductStockDataPort` exactly as it will with real Pricing and Inventory contexts;
   in stage 1 both are answered by in-memory adapters seeded at start-up. The catalog's Open Host Service exposes
   `ProductArticleInfo` (name, image, current price, stock) so Cart and Checkout have one source. When Pricing and
   Inventory exist, price and stock leave the catalog's Api, Cart and Checkout declare `[Upstream("Pricing")]` and
   `[Upstream("Inventory")]`, and the stand-in adapters are deleted — no port, use case or aggregate changes.
3. **Customers are guests** (cookie-identified) until the Account context provides identities; `CustomerId` is
   already a context-local value object in Cart and Checkout, so nothing else moves.

## Consequences

- Positive: the ubiquitous language of the three contexts matches the Java sample's glossaries now; growing the
  sample means adding contexts, not rewriting them.
- Negative: the catalog Api temporarily carried data that is not the catalog's. Resolved — price and stock come
  from the Pricing and Inventory contexts through events and their Open Host Services, and since 2026-08-31 the
  catalog's Api no longer relays them at all: each consumer asks the context that owns the figure. The catalog
  still reads both, but only to present its own pages.
