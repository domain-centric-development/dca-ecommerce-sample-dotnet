> **Status:** Bootstrap Draft — automatically extracted from source code.
> Must be ratified by a domain expert before terms become binding. In
> particular, lifecycle terms (Checked Out vs. Completed) and Item-vs.-Product
> usage need sharpening.

# Bounded Context: Cart (Shopping Cart)

The Cart context manages a customer's shopping cart from adding individual
items to triggering checkout. It references products from the Product context
via `ProductId` and retrieves current prices/availability through an
`IArticlePriceResolver` (anti-corruption layer to Pricing/Inventory).

## Aggregates

### ShoppingCart

**Definition:** A customer's shopping cart — a collection of items the
customer intends to purchase, with a lifecycle from Active through CheckedOut
to Completed or Abandoned.

**Type:** Aggregate Root

**Identity:** `CartId`

**Synonyms (avoid):** "Cart" on its own is ambiguous; always use
`ShoppingCart` when referring to the aggregate.

**Related terms:**
- `CartItem` — item within the shopping cart (Entity)
- `CartStatus` — lifecycle state
- `CustomerId` — owner of the shopping cart
- `IArticlePriceResolver` — external price/availability lookup
- `CartValidationResult` — result of checkout validation
- `EnrichedCart` — enriched read model

**Operations:** `Reconstitute`, `AddItem`, `RemoveItem`, `RemoveItemByProductId`,
`UpdateItemQuantity`, `IncreaseItemQuantity`, `DecreaseItemQuantity`, `Clear`,
`Checkout`, `Abandon`, `Complete`, `Merge`, `CalculateTotal`,
`ValidateForCheckout`

**Notes:** A `ProductId` may only appear once — adding it again increases the
quantity. Modifications are only allowed in state Active. `Reconstitute` restores a
stored cart — status and lines as they were — without re-evaluating a rule and
without raising an event: a stored cart is a fact, not a decision. Repositories hand
over the lines as `ShoppingCart.StoredItem`, so `CartItem`'s constructor can stay
internal and only the aggregate assembles its own lines.

## Entities

### CartItem

**Definition:** A single item in the shopping cart with a reference to the
product, the desired quantity, and the price captured at the time it was added
(price snapshot).

**Type:** Entity

**Identity:** `CartItemId`

**Synonyms (avoid):** "Line Item", "Position" — prefer `CartItem`.

**Related terms:** `Quantity`, `ProductId`, `Price` (Shared Kernel),
`PriceAtAddition`

**Operations:** Assembly-internal only, via `ShoppingCart`: `UpdateQuantity`,
`IncreaseQuantity`, `DecreaseQuantity`

**Notes:** Exists only within a `ShoppingCart` aggregate; no dedicated
repository. A repository restoring a stored cart passes `ShoppingCart.StoredItem`
values to `ShoppingCart.reconstitute` rather than constructing `CartItem` itself.

## Value Objects

### CartId

**Definition:** Unique identifier of a `ShoppingCart`.

**Type:** Value Object (Id)

### CartItemId

**Definition:** Unique identifier of a cart item within a `ShoppingCart`.

**Type:** Value Object (Id)

### CustomerId

**Definition:** Identifier of the customer who owns the cart. Reference into
the Customer/Account context.

**Type:** Value Object (Id)

**Notes:** Open question — should this type move to the Shared Kernel if
further contexts reference it?

### Quantity

**Definition:** Positive item count of a cart item (`> 0`). Carries
arithmetic operations (`Increase`, `Decrease`, `Add`) with
protection against values ≤ 0.

**Type:** Value Object

### CartStatus

**Definition:** Lifecycle state of a shopping cart: `Active` (modifiable),
`CheckedOut` (checkout triggered, locked), `Completed` (order confirmed), or
`Abandoned` (given up by the customer).

**Type:** Value Object (Enum)

**Notes:** Open question — clarify the relationship between `CheckedOut` and
`Completed`. Today `CheckedOut` is an intermediate state before `Completed`,
but `Complete()` may also be invoked directly from `Active`. Does the domain
expert expect a strict state machine?

### ArticlePrice

**Definition:** Snapshot of price, availability, and stock level of an article
— delivered by the `IArticlePriceResolver`.

**Type:** Value Object

### CartArticle

**Definition:** The Cart context's view of an article with name, current
price, available stock, availability status, and image URL. Required for
enriched read models.

**Type:** Value Object

**Synonyms (avoid):** Not to be confused with `ProductArticle` from the
Product context — both are modeled context-locally.

### CartValidationResult

**Definition:** Result of a checkout validation. Optionally contains a list
of validation errors per item (unavailable product, insufficient stock).

**Type:** Value Object

**Related terms:** `CartValidationResult.ValidationError`,
`CartValidationResult.ErrorType`

### EnrichedCart

**Definition:** Enriched read model of the shopping cart with current article
data. Knows cross-context rules such as price-change detection, checkout
eligibility, and subtotal calculation using current vs. original prices.

**Type:** Value Object (Read Model)

### EnrichedCartItem

**Definition:** Enriched item from `EnrichedCart`, combining `CartItem` data
(quantity, original price) with current `CartArticle` data — enables price
comparison, stock checks, and line-total calculation.

**Type:** Value Object

## Domain Events

### CartItemAddedToCart

**Definition:** An item was added to the shopping cart, or the quantity of an
existing item was increased by adding it again.

**Type:** Domain Event

**Synonyms (avoid):** Former names `ItemAddedToCart`, `CartItemAdded`.

### ProductRemovedFromCart

**Definition:** A product was completely removed from the shopping cart (all
related items deleted).

**Type:** Domain Event

**Synonyms (avoid):** `CartItemRemoved` — do not use in parallel; flagged as
a duplicate in the DCA review. If item-level removal is to be modeled
separately, clarify with the domain expert.

### CartItemQuantityChanged

**Definition:** The quantity of an existing cart item was changed (increased
or decreased). Carries the old and new quantity.

**Type:** Domain Event

### CartCleared

**Definition:** All items were removed from the shopping cart at once (e.g.,
via "clear cart").

**Type:** Domain Event

### CartCheckedOut

**Definition:** The checkout process was triggered — the shopping cart is
closed and no longer modifiable. Contains a snapshot of total and items for
integration into other contexts (notably Checkout/Order).

**Type:** Domain Event

**Synonyms (avoid):** `CartCompleted` — functionally different (see there).

**Notes:** Cross-context propagation occurs via the integration event
`CartCheckedOutEvent` in the outgoing adapter, not via this Domain Event
directly.

### CartCompleted

**Definition:** The entire checkout process including customer confirmation
(payment/review) is finished. Final state for successfully processed shopping
carts.

**Type:** Domain Event

**Synonyms (avoid):** Do not use synonymously with `CartCheckedOut` —
`CheckedOut` marks the triggering, `Completed` the final closing. Open
question for the domain expert: should these two events be merged
functionally, or does the distinction remain?

### CartAbandoned

**Definition:** The shopping cart was abandoned by the customer (e.g., after
an inactivity threshold). Final state.

**Type:** Domain Event

## Domain Services

### CartTotalCalculator

**Definition:** Calculates cart totals including taxes (default 19% VAT),
shipping costs, and combined gross total.

**Type:** Domain Service

## Specifications

### ICartSpecification

**Definition:** Common interface of all cart-related specifications; enables
persistence adapters to translate them into queries via the Visitor pattern
(`ICartSpecificationVisitor`).

**Type:** Specification (Marker)

### ActiveCart

**Definition:** Shopping cart is in state `Active`.

**Type:** Specification

### LastUpdatedBefore

**Definition:** Shopping cart was last updated before a given timestamp (for
abandoned-cart detection).

**Type:** Specification

**Notes:** In-memory evaluation is currently neutral because the aggregate
does not carry an `UpdatedAt` — the persistence adapter evaluates the
predicate push-down. Open question: should `UpdatedAt` be added to the
aggregate?

### HasMinTotal

**Definition:** Cart total reaches or exceeds a minimum value (e.g., for a
minimum order amount or free shipping).

**Type:** Specification

### HasAnyAvailableItem

**Definition:** Shopping cart contains at least one sellable item (available,
not discontinued).

**Type:** Specification

**Notes:** In-memory neutral (true), since the domain model has no notion of
availability — the adapter evaluates this via joins.

### CustomerAllowsMarketing

**Definition:** The customer owning the cart has consented to marketing
communication (opt-in).

**Type:** Specification

**Notes:** In-memory neutral. Open question: does marketing consent belong
functionally in the Cart context at all, or is this a cross-context query to
Account/Customer?

## Factories

### EnrichedCartFactory

**Definition:** Builds an `EnrichedCart` from a `ShoppingCart` aggregate and a
map of current article data (`CartArticle` per `ProductId`). Validates
completeness of the article data.

**Type:** Factory

## Ports / Interfaces

### IArticlePriceResolver

**Definition:** Interface through which the Cart context retrieves
current prices and availabilities for a product — without direct coupling to
Pricing or Inventory infrastructure. Used in the aggregate for
`CalculateTotal(...)` and `ValidateForCheckout(...)`.

**Type:** Concept (Domain Port)

**Related terms:** `ArticlePrice`

## Concepts (no code artifact)

### Price Snapshot (PriceAtAddition)

**Definition:** The price of a cart item frozen at the time it was added.
Serves price-change detection and transparency toward the customer, but is
NOT the price used for settlement at checkout time (that one is fetched fresh
from the `IArticlePriceResolver`).

**Type:** Concept

**Notes:** Open question: which price is binding for the customer — the one
at the time of adding or the current one? Clarify the business policy.
