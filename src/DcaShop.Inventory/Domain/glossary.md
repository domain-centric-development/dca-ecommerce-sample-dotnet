# Inventory Context — Ubiquitous Language Glossary

> **Bootstrap note:** This glossary was initially derived from the existing
> domain code (`inventory/domain/model` and `inventory/domain/event`). It
> reflects the **current state**, including known language and modeling
> conflicts (see the "Open Issues" section). Terms are intended to be aligned
> and consolidated iteratively with the business. Update via
> `/ubiquitous-language`.

Language: definitions in English, class names unchanged (English).

---

## Aggregates

### StockLevel

**Definition:** Stock-keeping unit for exactly one product. Manages the
available quantity in the warehouse as well as the portion of it reserved for
open orders, and ensures that reservations never exceed the available quantity.

**Type:** Aggregate Root

**Identity:** `StockLevelId`

**Related terms:** `StockQuantity`, `ProductId`, `StockLevelCreated`,
`StockIncreased`, `StockDecreased`, `StockReserved`, `StockReleased`,
`StockChanged`

**Operations:**
- `Create(productId, initialQuantity)` — Create a new stock-keeping unit
- `IncreaseStock(amount)` — Record incoming goods
- `DecreaseStock(amount)` — Record outgoing goods (e.g. shipment)
- `Reserve(amount)` — Reserve stock for an order
- `Release(amount)` — Release previously reserved stock
- `AdjustStockTo(quantity)` — Manual correction / stocktake (see Open Issues)
- `IsAvailable` — Says whether unreserved stock is available

**Notes:** Invariant: `reservedQuantity <= availableQuantity`. If the available
stock drops below the reserved amount, the reservation is automatically
adjusted downward.

---

## Entities

_No standalone entities besides the aggregate root._

---

## Value Objects

### StockQuantity

**Definition:** Non-negative count of items in the warehouse. Serves as a
type-safe wrapper around a quantity and encapsulates the invariant
"quantity >= 0".

**Type:** Value Object

**Identity:** _value-based (`Value: int`)_

**Related terms:** `StockLevel`

**Operations:**
- `Of(value)` — Factory
- `Value` — Access to the raw value

**Notes:** Currently `int`-based. Arithmetic is currently performed outside the
value object in the aggregate — a candidate for relocation into the value
object.

---

### StockLevelId

**Definition:** Stable, context-local identity of a stock-keeping unit.

**Type:** Value Object (Identity)

**Identity:** UUID-based

**Related terms:** `StockLevel`

**Operations:** `Generate()`, `Of(guid)`

---

## Domain Events

### StockLevelCreated

**Definition:** A new stock-keeping unit has been created for a product.

**Type:** Domain Event

**Related terms:** `StockLevel.create`

**Notes:** Payload: `StockLevelId`, `ProductId`, `InitialQuantity`.

---

### StockIncreased

**Definition:** Available stock was increased by a specific amount
(typical trigger: incoming goods).

**Type:** Domain Event

**Related terms:** `StockLevel.increaseStock`

---

### StockDecreased

**Definition:** Available stock was reduced by a specific amount
(typical trigger: shipment / outgoing goods).

**Type:** Domain Event

**Related terms:** `StockLevel.decreaseStock`

---

### StockReserved

**Definition:** A portion of the available stock has been reserved for an open
order and is no longer available for further reservations.

**Type:** Domain Event

**Related terms:** `StockLevel.reserve`

---

### StockReleased

**Definition:** A previously made reservation has been cancelled; the quantity
is reservable again.

**Type:** Domain Event

**Related terms:** `StockLevel.release`

---

### StockChanged

**Definition:** Stock was set to a target value as part of a manual correction
or stocktake (delta-agnostic).

**Type:** Domain Event

**Synonyms (avoid):** _generic catch-all term_ — per the DCA review, should be
replaced by more business-precise events (`StockReconciled` for stocktake), or
reduced entirely to `StockIncreased`/`StockDecreased`.

**Related terms:** `StockLevel.adjustStockTo`

**Notes:** See Open Issues — "too generic".

---

## Domain Services

_None._

---

## Specifications

### IStockLevelSpecification

**Definition:** Common interface of all stock-related specifications; enables
persistence adapters to translate them into queries via the Visitor pattern
(`IStockLevelSpecificationVisitor`).

**Type:** Specification (Marker)

### AvailableQuantityBelow

**Definition:** The available quantity of a stock level is strictly below a
given threshold — the stock level is *low*. The threshold itself is not low.

**Type:** Specification

**Related terms:** `StockQuantity`, Available quantity, Low stock, Stock
threshold

**Notes:** Compares the quantity **held**; reservations do not enter the
comparison (see Open Issues). Evaluated in memory by the default
`IStockLevelRepository.FindByAsync`; the visitor exists so an adapter can adopt
predicate push-down without query technology entering the domain.

---

## Factories

_No separate factories — construction via the static factory method
`StockLevel.create(...)`._

---

## Integration Contracts

### IStockInitializationTrigger

**Definition:** The shape Inventory asks of any event that means "this product must have a stock level
now": the product and the quantity it starts with. Inventory owns the contract; the Product Catalog
implements it on its `ProductCreatedEvent` (Interface Inversion), so Inventory never depends on the
catalog.

**Type:** Consumer-defined trigger contract (Published Language, owned here)

**Related terms:** `StockLevel`, `IStockReductionTrigger`

**Notes:** Consumed by `DcaShop.Inventory.Adapter.Incoming.Event.StockInitializationEventConsumer`, which calls
the `SetStockLevel` use case. Idempotent: setting the same figure twice has the same effect.

---

### IStockReductionTrigger

**Definition:** The shape Inventory asks of any event that means "stock has been sold": the line items
whose stock must go down. Owned here and implemented by Checkout's `CheckoutConfirmedEvent`.

**Type:** Consumer-defined trigger contract (Published Language, owned here)

**Related terms:** `StockLevel`, `IStockInitializationTrigger`

**Notes:** Consumed by `DcaShop.Inventory.Adapter.Incoming.Event.StockReductionEventConsumer`. Its
`OrderLineItem` wording is an open issue — see below.

---

## Concepts (not in code, but in conversation)

### Available quantity

**Definition:** Quantity of a product physically held in the warehouse,
regardless of whether parts of it are already reserved.

**Type:** Concept

**Related terms:** `StockQuantity`, `StockLevel.availableQuantity`

---

### Reserved quantity

**Definition:** Portion of the available stock that is blocked for orders not
yet shipped and is no longer available for further reservations.

**Type:** Concept

**Related terms:** `StockQuantity`, `StockLevel.reservedQuantity`

---

### Unreserved quantity (available-to-promise)

**Definition:** `availableQuantity − reservedQuantity` — the quantity that can
still be reserved.

**Type:** Concept

**Notes:** Currently not modeled as a value object; candidate for extraction.

---

### Low stock

**Definition:** State of a stock level whose available quantity is strictly
below a threshold the asker gives — the state an operator must react to before
the product runs out.

**Type:** Concept

**Related terms:** `AvailableQuantityBelow`, Available quantity, Stock
threshold

**Notes:** Realised by `AvailableQuantityBelow`; asked for through the
`GetLowStockProducts` use case, which answers a `LowStockProduct` per stock
level with the quantity it holds.

---

### Stock threshold

**Definition:** The quantity, supplied by the asker, at or above which a stock
level is not low.

**Type:** Concept

**Related terms:** Low stock, `AvailableQuantityBelow`

**Notes:** Not a property of a product: no stock level carries a reorder level
of its own (see Open Issues). Carried per request by
`GetLowStockProductsQuery.Threshold`.

---

## Open Issues (from DCA review)

These points are intentionally left open and should be clarified with the
business before the model is consolidated:

1. **Use-case split for `AdjustStockTo`** (formerly `SetAvailableQuantity`) — the method conflates
   three business-distinct operations:
   - **Set** (initial capture / master-data correction)
   - **Adjust** (correction posting with reason)
   - **Reconcile** (stocktake reconciliation)

   Recommendation: split the method and emit an appropriate event per trigger.

2. **`StockChanged` is too generic** — should be replaced by business-meaningful
   events such as:
   - `StockReconciled` (stocktake)
   - or a clean separation via `StockReserved` / `StockReleased` /
     `StockIncreased` / `StockDecreased`, which already exist anyway.

3. **`IStockReductionTrigger.OrderLineItem`** is **Checkout language** and does
   not belong in the Inventory context. From an Inventory perspective, the
   trigger of a stock reduction should be generic (e.g. "shipment order",
   "reservation release") — correlation to the order ID happens via
   integration events, not via borrowed terms.

4. **Arithmetic on `StockQuantity`** is currently done in the aggregate.
   Candidate for moving addition and subtraction into the value object.

5. **Which quantity is "low"?** `AvailableQuantityBelow` compares the
   available quantity, so a stock level of 4 with 4 units reserved is not low
   at a threshold of 4. Whether the operator means available quantity or
   unreserved quantity (available-to-promise) is unanswered; both readings are
   in this glossary.

6. **Whose threshold?** One threshold is supplied per request. Whether a stock
   level should instead carry its own reorder level is unanswered.
