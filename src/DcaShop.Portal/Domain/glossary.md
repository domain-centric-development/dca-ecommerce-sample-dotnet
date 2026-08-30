# Portal — Ubiquitous Language (Bootstrap)

> **Bootstrap status:** This glossary was initially derived from the existing code.
> Portal has **no domain model of its own** — it is a pure UI shell that provides
> navigation and the landing page and *displays* concepts of other Bounded
> Contexts but does not *own* them. No terms are invented here on purpose;
> instead, this glossary lists the concepts the Portal refers to.

## Module Character

According to the `PortalContext` marker class, Portal is marked as a **Bounded Context**
(`[BoundedContext("Portal")]`), but functionally it is a **UI composition
shell**:

- A landing page (`HomePageController`, `GET /`)
- Navigation to Product Catalog, Shopping Cart, Checkout
- No Aggregates, Entities, Value Objects, Events, or Domain Services
- No application use cases (purely presentational)
- It references no other context project — only `DcaShop.SharedKernel`

**Classification recommendation:** Portal more closely matches a **Generic
Subdomain for UI composition** than a business Bounded Context in the DDD
sense. See "Open Questions" below.

---

## Own Terms

### HomePage

**Definition:** Entry page of the e-commerce portal. Displays the application
title and navigation elements for products, cart, and checkout.

**Type:** Concept (UI view, not a domain concept)

**Operations:** `GET /` → `Views/Home/Index.cshtml`

**Notes:** Currently the module's only endpoint.

---

## Referenced Terms (displayed from other contexts)

These terms **do not belong to Portal** — it only displays them or links to
them. Definitions can be found in the respective context glossaries.

| Term              | Owning Context        | Usage in Portal                               |
|-------------------|-----------------------|-----------------------------------------------|
| Product / Catalog | `Product`             | Navigation to product listing                 |
| Cart              | `Cart`                | Navigation to shopping cart                   |
| Checkout / Order  | `Checkout`            | Navigation to checkout flow                   |
| Account / User    | `Account`             | Login, register and profile entry points      |

---

## Open Questions

- **Bounded Context or Generic Subdomain?** Portal is marked as
  `[BoundedContext]` in the code but has **no domain model**. Recommendation:
  classify it as a **Generic Subdomain** (UI shell / composition) and possibly
  rename the marker to something more appropriate (`@GenericSubdomain`,
  `@UiModule`, or keep it with clear documentation).
- Should Portal eventually get its own view model for cross-context dashboards
  (e.g. "Recommendations", "My Dashboard")? If so, real application use cases
  would emerge here — but still no Aggregates of its own, only read models
  derived from other contexts.
- How will Portal handle aggregated views once they become necessary — via
  integration events plus a local read model, or via Open Host Services of
  other contexts?
