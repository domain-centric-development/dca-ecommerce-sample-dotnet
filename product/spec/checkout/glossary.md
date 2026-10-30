# Glossary — Bounded Context: Checkout

> **Bootstrap draft** — Automatically extracted from the current domain code.
> Terms reflect the as-is implementation, not necessarily the target Ubiquitous Language.
> Conflicts and polysemy are marked under "Notes" and should be resolved with the business
> stakeholder before this draft is adopted as authoritative.

Sources: `DcaShop.Checkout.Domain.{Model,Event,Service,ReadModel}`.

---

## Features (application layer)

The Checkout context groups its use cases into four features — domain-named navigation groups below
`Application/`, not model boundaries; every feature works on the one `CheckoutSession` aggregate.

| Feature | Meaning | Use cases |
|---------|---------|-----------|
| `Session` | Starting a checkout session from a cart and reading it back in its lifecycle states | `StartCheckout`, `GetActiveCheckoutSession`, `GetCheckoutSession`, `GetConfirmedCheckoutSession` |
| `CheckoutCompletion` | The steps that complete a checkout: buyer information, delivery, payment, confirmation — and resuming the cart's open checkout on a step page | `ResumeCheckout`, `SubmitBuyerInfo`, `GetShippingOptions`, `SubmitDelivery`, `GetPaymentProviders`, `SubmitPayment`, `GetReview`, `ConfirmCheckout` |
| `CartSync` | Following changes of the underlying cart while the session is active | `SyncCheckoutWithCart` |
| `OrderConfirmation` | Mailing the buyer that the order went through, once the checkout is confirmed | `SendOrderConfirmation` |

`Application/Shared/` stays context-wide. The web adapter mirrors the features: `Adapter/Incoming/Web/Session/`
holds `StartCheckoutPageController`, `Adapter/Incoming/Web/CheckoutCompletion/` one page controller per step page
(`BuyerInfo-`, `Delivery-`, `Payment-`, `Review-`, `ConfirmationPageController`), each with its own view model;
`CheckoutRoutes`, `CheckoutStepNotice` and `CartNotice` (the codes the cart page knows) stay beside them, shared.
`CheckoutStepPage` in `CheckoutCompletion/` is the one place a step page is opened, the cart's open checkout
resumed when the customer has no session, or a refused submission translated. The event consumers
sit in `Adapter/Incoming/Event/CartSync/` and `Adapter/Incoming/Event/OrderConfirmation/`.

---

## Aggregate Roots

### CheckoutSession

An explicit checkout action captures immutable positions and quantities, and the prices of that moment, into a session. Cart edits do not
create or mutate sessions. A new action supersedes the previous OPEN/Active session; confirmed/completed orders remain.
Confirmation and replacement serialize through the same repository operation, including transaction completion.
Superseded confirmation has no completion effect. Abandonment/expiry closes only an open session and leaves cart contents.

Cart reconciliation intersects purchased unit intervals with the current stable position id. Later additions (also of
the same product), removed/re-added positions and other contents survive. Replay and overlapping completed snapshots
cannot remove a unit twice. JDBC/JPA cart persistence preserves the interval allocation watermark; in-memory persistence
retains the same domain state. Legacy CheckedOut/Completed cart statuses remain readable, but snapshot checkout leaves
an active cart editable and never completes the whole cart.

Confirmation retrieves current price/availability/stock facts before its local transaction. Pure domain services consume
immutable line/fact snapshots. The review shows the order at today's prices and changes nothing; "Place Order" carries
the order fingerprint the review showed. A shortage, or an order whose fingerprint at today's prices differs from the one
shown, is refused and leaves state, totals and events unchanged; the review then shows today's order, and pressing
"Place Order" again orders it. Success stores the lines at today's prices with their total and publishes the same total;
there is no confirmation without a fingerprint. Local in-memory repository serialization is not a
claim of durable distributed transactions or universal rollback of unenlisted resources.

**Type:** Aggregate Root

**Operations:**
- `Start` — the session comes into existence over a snapshot of the cart's positions, quantities and prices
- `Supersede` — kept with status superseded when a new checkout action replaces it; no event; only an active session can be superseded
- `SubmitBuyerInfo`, `SubmitDelivery`, `SubmitPayment` — record the step's data and move on to the next step when standing on it.
  `SubmitPayment` records only the chosen provider: no payment is requested and no event is published; a total of zero
  is refused (`NothingToPay`)
- `GoBackTo` — moves the current step back; data already submitted stays
- `LinesAt`, `TotalsAt`, `FingerprintAt` — questions: the lines, the totals and the order fingerprint at today's prices, as the article facts state them; a line whose article is not available keeps its own price
- Get review (`GetReview`) — the use case the review page reads: the session at today's prices with its order fingerprint; no transaction, nothing stored
- `AssertReadyForConfirmation` — a question: the preconditions of `Confirm` against a pricing verdict and the order fingerprint shown, checked without changing anything
- `Confirm` — status confirmed, the lines and totals at today's prices, the authorized payment recorded; publishes
  `PaymentSubmitted`, then `CheckoutConfirmed`. Refused while the pricing verdict reports a shortage, when the order
  fingerprint at today's prices differs from the one the review showed (`PriceChangedSinceReview` names the first line
  that differs, `OrderChangedSinceReview` anything else), and for a payment that does not pay exactly the confirmed
  total through the chosen provider (`PaymentDoesNotCoverTotal`). Place Order (`ConfirmCheckout`) checks the session
  and the fingerprint, reserves its stock in Inventory, has the chosen provider authorize the total the review showed,
  then confirms: an order changed since the review, or a line the stock no longer covers, is refused and requests no
  payment; a payment refused or unanswered, or a confirmation that then fails, releases the reservation
- `Complete` — after payment: status completed, with the order reference
- `Abandon` — kept with status abandoned; the cart's contents stay
- `Expire` — kept with status expired, for inactivity; the cart's contents stay
- Hand over the checkout (`HandOverTo`) — the open session follows its cart to the customer who owns the cart now: a
  guest's checkout becomes the account's when the guest cart was handed over at a login. Cart, lines and the step
  reached stay as they are; only an open session is handed over (else `CheckoutNotModifiable`)
- Resume checkout (`ResumeCheckout`) — the use case a step page runs when the customer has no session of their own:
  it finds the open session of the customer's active cart (which cart that is, Cart's Open Host Service says) and
  hands it to the customer. The one step-page GET that updates a session; it never creates one, so a cart without an
  open checkout still leads to the cart (ADR-012)
- `SyncLineItems` — exists only to refuse: a session's positions are immutable, a changed cart needs a new session


### BuyerInfo

**Definition:** Contact details of the buyer (email, first and last name, phone) captured in the
first checkout step in order to process the order.

**Type:** Value Object

**Synonyms (avoid):** `Buyer`, `BuyerContact` — both appear in documentation/discussion but are
not modelled in code.

**Related terms:** `CheckoutSession`, `BuyerInfoSubmitted`.

**Notes:** Conflicts with `LineItemInfo` in the `CheckoutConfirmed` event — both names use the
`Info` suffix for different concepts (buyer vs. order line item in the event). Possible
consolidation: `Buyer` for the VO, `LineItem` (rename the event variant unambiguously).

### DeliveryAddress

**Definition:** Shipping address consisting of street, optional second street line, city,
postal code, country, and optional state. Captured in the delivery step.

**Type:** Value Object

**Related terms:** `CheckoutSession`, `ShippingOption`, `DeliverySubmitted`.

**Operations:** `FormattedAddress`.

### Shipping option (ShippingOption)

**Definition:** A way of shipping the shop offers at checkout — its title (for example "Standard",
"Express"), the estimated delivery and the cost the shop sets, never one the buyer sends. The buyer
selects one. Agent commerce protocols call it a fulfillment option of type shipping (UCP, ACP) or a
shipping option (W3C Payment Request); a carrier, where one is named, is a field of its own.

**Type:** Value Object

**Related terms:** `DeliveryAddress`, `CheckoutTotals`, `DeliverySubmitted`.

**Operations:** `IsFree`.

### PaymentSelection

**Definition:** Selected payment method: the `PaymentProviderId` chosen at the payment step and, once the provider
authorized the payment at Place Order, the provider reference (e.g. payment intent ID) and the authorized amount.

**Type:** Value Object

**Related terms:** `PaymentProviderId`, `PaymentSubmitted`, Authorized amount, Payment cancellation.

**Operations:** `Authorized` — the chosen way to pay with the reference and the amount its provider authorized;
`WithReference`, `HasReference`, `Covers` — whether the payment, authorized by the given provider,
pays exactly the given total: it needs a payment reference, the same provider and an authorized amount equal to the
total.

### Authorized amount (AuthorizedAmount)

**Definition:** The total the review showed, authorized by the payment provider at Place Order. The order is
confirmed only when it covers the confirmed total (`PaymentDoesNotCoverTotal` otherwise).

**Type:** Value Object (`Money`, part of `PaymentSelection`)

**Related terms:** `PaymentSelection`, `CheckoutTotals`, Payment cancellation.

### CheckoutLineItem

**Definition:** A line item within the checkout session — contains product reference, label,
unit price (captured when the checkout started; once confirmed, the price the order was placed at), quantity, and an
optional product image.

**Type:** Value Object

**Identity:** `CheckoutLineItemId`

**Related terms:** `CheckoutSession`, `EnrichedCheckoutLineItem`, `LineItemSnapshot`.

**Operations:** `LineTotal`, `WithQuantity`, `WithUnitPrice`.

**Notes:** Deliberately modelled as a Value Object (no separate entity) because identity is only
relevant within the `CheckoutSession`. Conflict: `LineItemInfo` (in the `CheckoutConfirmed` event)
and `LineItemSnapshot` (read model) structurally duplicate parts of the same content.

### CheckoutArticle

**Definition:** The checkout context's view of an article from other contexts — contains current
price, availability, and stock data needed for validation and price display during checkout.

**Type:** Value Object

**Related terms:** `EnrichedCheckoutLineItem`, `CheckoutCart`, `ICheckoutArticlePriceResolver`.

**Operations:** `HasStockFor`.

**Notes:** Anti-Corruption Layer representation; no direct coupling to Product Catalog, Inventory,
or Pricing.

### EnrichedCheckoutLineItem

**Definition:** Combination of a historical `CheckoutLineItem` line and current `CheckoutArticle`
data; enables business comparisons such as price change since addition and sufficient stock.

**Type:** Value Object

**Related terms:** `CheckoutLineItem`, `CheckoutArticle`, `CheckoutCart`.

**Operations:** `CurrentLineTotal`, `OriginalLineTotal`, `HasPriceChanged`, `PriceDifference`,
`HasSufficientStock`, `IsValidForCheckout`.

### CheckoutCart

**Definition:** "Smart Shopping Cart" — enriched view of all line items in a checkout session
including current prices, price-change detection, and validity checking. Serves as a
read-optimized view for the summary and review screens.

**Type:** Value Object

**Related terms:** `CartId`, `CustomerId`, `EnrichedCheckoutLineItem`, `CheckoutCartFactory`,
`CheckoutCartSnapshot`.

**Operations:** `CalculateCurrentSubtotal`, `CalculateOriginalSubtotal`, `TotalPriceDifference`,
`HasAnyPriceChanges`, `ItemsWithPriceChanges`, `IsValidForCheckout`, `InvalidItems`,
`UnavailableItems`, `ItemsWithInsufficientStock`, `ItemCount`, `TotalQuantity`.

**Notes:** Conflicts with `CheckoutSession.LineItems` — both represent "cart inside checkout",
but `CheckoutSession` holds raw snapshots while `CheckoutCart` holds the enriched view. Also
structurally overlaps with `CheckoutCartSnapshot` (read model). Possible cleanup: rebrand
`CheckoutCart` as a pure review/display construct or unify it with the snapshot. External DTOs
`CartData`/`CartItemData` should be renamed in favor of `CartSnapshot`.

### CheckoutTotals

**Definition:** Calculated amounts of a checkout session: subtotal, shipping, tax, and the order
total (`Total`) — the amount of the order, not an amount paid: the payment is only authorized.

**Type:** Value Object

**Related terms:** `CheckoutSession`, `Money`, `ShippingOption`.

**Operations:** `Of`, `Calculate`, `Zero`, `WithShipping`, `WithTax`.

### CheckoutValidationResult

**Definition:** Result of validating the line items against current availability and stock data;
contains a list of `ValidationError` entries.

**Type:** Value Object

**Related terms:** `ValidationError`, `ICheckoutArticlePriceResolver`, `CheckoutSession`.

**Operations:** `IsValid`, `Valid`, `WithErrors`, `WithError`, `ShortOfStock` (the first line, in line
order, that is unavailable or short of stock).

### ValidationError

**Definition:** A specific violation of checkout preconditions for a line item (e.g. product
unavailable, insufficient stock), naming the line's product by its id and by its name.

**Type:** Value Object

**Related terms:** `CheckoutValidationResult`, `ErrorType`, `ProductId`, `ProductName`.

**Operations:** `ProductUnavailable`, `InsufficientStock`.

### CheckoutStep

**Definition:** Step in the checkout flow (`BuyerInfo`, `Delivery`, `Payment`, `Review`,
`Confirmation`) with a defined order and navigation rules.

**Type:** Value Object (Enum)

**Operations:** `IsBefore`, `IsAfter`, `IsTerminal`, `Next`, `Previous`.

### StepAccess

**Definition:** The domain's answer to "may this checkout step be opened?": granted, redirect to
another step, or back to the cart (no usable session).

**Type:** Value Object

**Related terms:** `CheckoutStep`, step access on `CheckoutCartSnapshot`.

**Operations:** `Grant()`, `RedirectTo(step)`, `BackToCart()`, `Granted`, `IsBackToCart`.

**Notes:** Carries a step, never a URL — the incoming adapter turns it into a route.

### CheckoutSessionStatus

ACTIVE/Active is the open, modifiable session. CONFIRMED/Confirmed may complete; COMPLETED/Completed is final. SUPERSEDED/Superseded, ABANDONED/Abandoned and EXPIRED/Expired reject confirmation. Only an open session may be replaced, abandoned or expired.

### ErrorType

**Definition:** Classification of a validation error (`ProductUnavailable`,
`InsufficientStock`).

**Type:** Value Object (Enum)

### CheckoutSessionId

**Definition:** Unique identifier of a checkout session.

**Type:** Value Object (ID)

**Operations:** `Generate`, `Of`.

### CheckoutLineItemId

**Definition:** Unique identifier of a line item within a checkout session.

**Type:** Value Object (ID)

### CartId

**Definition:** Context-local identifier of the underlying shopping cart.

**Type:** Value Object (ID)

**Notes:** Deliberate copy of the external cart identity; avoids direct coupling to the Cart
bounded context.

### CustomerId

**Definition:** Context-local identifier of the customer (may be a guest session).

**Type:** Value Object (ID)

**Notes:** Polysemy — the same name also exists in the Cart context and is semantically related
to `UserId`/`AccountId` in the Account context. To be resolved whether `UserId` from the Shared
Kernel should be used here.

### PaymentProviderId

**Definition:** Identifier of an external payment provider. The shop registers a single one:
`provider`, the payment provider behind its REST contract, where a provider address
(`Checkout:PaymentProvider:BaseUrl`) is configured; `mock`, the payment stand-in, otherwise.

**Type:** Value Object (ID)

**Related terms:** `PaymentResult`, `PaymentSelection`.

### ArticlePrice

**Definition:** Bundle of current article price, availability, and stock level returned by the
`ICheckoutArticlePriceResolver`.

**Type:** Value Object

**Related terms:** `ICheckoutArticlePriceResolver`.

### Order fingerprint (OrderFingerprint)

**Definition:** What the review showed: each line's product, quantity and unit price, the buyer, the address, the
shipping option and its price, the payment method and the total — carried by "Place Order". A different one at that
moment refuses the order; a missing or garbled one matches no order.

**Type:** Value Object

**Related terms:** `CheckoutSession`, Get review, `PriceChangedSinceReview`, `OrderChangedSinceReview`.

**Operations:** `Of`, `From`, `FirstChangedLine` — the first line whose digest differs from the fingerprint shown; none
when the lines agree or their number differs.

---

## Domain Events

### CheckoutSessionStarted

**Definition:** A new checkout session has been started from a shopping cart.

**Type:** Domain Event

**Related terms:** `CheckoutSession`, `CartId`, `CustomerId`.

### BuyerInfoSubmitted

**Definition:** Buyer details have been submitted in the first step.

**Type:** Domain Event

**Related terms:** `BuyerInfo`, `CheckoutSession`.

### DeliverySubmitted

**Definition:** Delivery address and shipping option have been submitted.

**Type:** Domain Event

**Related terms:** `DeliveryAddress`, `ShippingOption`.

### PaymentSubmitted

**Definition:** The payment provider authorized the order's total at Place Order; raised with `CheckoutConfirmed`,
just before it.

**Type:** Domain Event

**Related terms:** `PaymentSelection`, `PaymentProviderId`.

### CheckoutConfirmed

**Definition:** The customer has confirmed the order at the review step; the order is finally
assembled.

**Type:** Domain Event

**Related terms:** `LineItemInfo`, `CheckoutSession`, `CartId`, `CustomerId`.

**Notes:** Not published cross-context directly; an outgoing adapter creates the integration
event `CheckoutConfirmedEvent`. `LineItemInfo` (in the payload) conflicts with `CheckoutLineItem`
and `LineItemSnapshot` — all three structurally describe the same line item for different
purposes.

### CheckoutCompleted

**Definition:** Payment was processed successfully and the order has been finalized.

**Type:** Domain Event

**Related terms:** `Money`, `OrderReference`.

### CheckoutAbandoned

**Definition:** The customer has actively abandoned the checkout.

**Type:** Domain Event

**Related terms:** `CheckoutStep`.

### CheckoutExpired

**Definition:** The checkout session has been invalidated by the system due to inactivity.

**Type:** Domain Event

**Related terms:** `CheckoutStep`.

---

## Domain Services

### Step access

**Definition:** Whether a checkout may be shown at a requested step, and where the customer goes
instead: terminal and confirmed sessions only reach the confirmation, and a step whose prerequisites are
unfulfilled sends them to the step they are actually on. Going back to a completed step is allowed. A
session that is not there at all sends them back to the cart — the one part the model cannot answer,
because there is no snapshot to ask; the `GetCheckoutSession` use case answers that.

**Type:** Behaviour of `CheckoutCartSnapshot`

**Notes:** Business logic, not presentation: it holds whether the user interface shows one page or five.
It decides on the snapshot alone, so a read does not load the session aggregate.

**Related terms:** `CheckoutSession`, `CheckoutCartSnapshot`, `CheckoutStep`, `StepAccess`.

**Operations:** `AccessTo(targetStep)` — answers with a `StepAccess`.

**Notes:** Decides on the `CheckoutCartSnapshot` read model and knows no routes. The `GetCheckoutSession` query
use case invokes it when a step is requested (`GetCheckoutSessionQuery.ForStep`) and delivers the `StepAccess` in its result; the web
adapter (`CheckoutRoutes`) maps the decision to a redirect path. A step's form refused as sent too early
(`CheckoutStepNotCompletedException`, `CheckoutStepOutOfOrderException`) asks step access for that step too, and a
denied access sends the customer to the step still open, which says that the earlier steps come first.

### CheckoutArticlePriceResolver

Legacy application/test lookup abstraction. CheckoutSession accepts supplied article facts; CheckoutPricing owns cross-article validation of availability and stock. No repository or remote callback enters the aggregate.

### CheckoutCartFactory

**Definition:** Creates a `CheckoutCart` instance from checkout line items and corresponding
current article data, validating the completeness of the input.

**Type:** Factory

**Related terms:** `CheckoutCart`, `CheckoutLineItem`, `CheckoutArticle`, `EnrichedCheckoutLineItem`.

**Operations:** `Create`, `FromSession`.

---

## Read Models

### CheckoutCartSnapshot

**Definition:** Immutable, query-optimized snapshot of a `CheckoutSession`'s state for display
and read purposes.

**Type:** Concept (Read Model / Value Object)

**Related terms:** `CheckoutSession`, `LineItemSnapshot`.

**Operations:** `From`, `AtTodaysPrices` (the session as the review shows it), `ItemCount`, `TotalQuantity`, `HasBuyerInfo`, `HasDeliveryAddress`,
`HasShippingOption`, `HasPaymentSelection`, `HasTotals`, `HasOrderReference`, `IsActive`,
`IsCompleted`, `IsConfirmed`.

### LineItemSnapshot

**Definition:** Immutable view of a line item from the `CheckoutCartSnapshot`.

**Type:** Concept (Read Model / Value Object)

**Related terms:** `CheckoutLineItem`, `CheckoutCartSnapshot`.

**Operations:** `LineTotal`.

**Notes:** External adapter DTOs `CartData` and `CartItemData` should be renamed to follow this
snapshot naming scheme (`CartSnapshot` / `CartItemSnapshot`).

---

## Open issues from DCA review

- **`BuyerInfo` vs `Buyer`/`BuyerContact`** — Unify the naming scheme; avoid the `Info` suffix.
- **`LineItemInfo` (in `CheckoutConfirmed`) vs `CheckoutLineItem` vs `LineItemSnapshot`** —
  three near-identical structures; define clear roles (event payload vs. aggregate content
  vs. read model).
- **`CheckoutCart` vs `CheckoutSession.LineItems`** — Resolve duplicate "cart" representations;
  consider turning `CheckoutCart` into a pure review/display VO or merging it with the snapshot.
- **`CartData` / `CartItemData` (adapter DTOs) → `CartSnapshot` / `CartItemSnapshot`** —
  Align with the naming convention.
- **`CustomerId` polysemy** — the same name exists in `Cart`, with semantic overlap to `UserId`/
  `AccountId` from `SharedKernel`/`Account`. Clarify which identity is authoritative.

### Shared contract revision (2026-09-09)

Price wraps strictly positive Money; Money is ISO 4217, non-negative, two decimals half-up, maximum 999999999999.99.
Default quantities must be rejected before mutation/reconstitution. ProductCreated is raised by aggregate creation;
product-created v1 exposes only eventId, occurredOn, productId, amount, currency and initialStock.

---

## Failures

Named refusals. The session states its own rules; the use case states what it could not get hold of.

### CheckoutNotModifiable / CheckoutNotConfirmed

**Definition:** A session that is no longer open would be changed, or one that was never confirmed would be
completed.

**Type:** Domain failure (`DomainException`) · **Related terms:** `CheckoutSessionStatus`

---

### CheckoutStepNotCompleted / CheckoutStepOutOfOrder / CheckoutStepNotNavigable

**Definition:** A step is missing the data a later one needs; a step ahead of the current one was asked for; the
confirmation step was navigated to directly.

**Type:** Domain failure (`DomainException`) · **Related terms:** `CheckoutStep`

---

### EmptyCheckout / NothingToPay

**Definition:** A session would start without line items, or a payment would be arranged for a total of zero.

**Type:** Domain failure (`DomainException`) · **Related terms:** `CheckoutLineItem`, `CheckoutTotals`

---

### PaymentDoesNotCoverTotal

**Definition:** An order would be confirmed with a payment that does not pay exactly its total through the provider
the customer chose. Carries the authorized amount and the total.

**Type:** Domain failure (`DomainException`) · **Related terms:** `PaymentSelection`, Authorized amount

---

### CheckoutValidation

**Definition:** At confirmation time a line item no longer passes validation — unavailable, or short of stock. Carries
the per-item result.

**Type:** Domain failure (`DomainException`) · **Related terms:** `CheckoutValidationResult`

---

### PriceChangedSinceReview / OrderChangedSinceReview

**Definition:** "Place Order" met an order whose fingerprint differs from the one the review showed. A line that
differs names its product: "The price of '<product>' has changed. Please check your order and place it again."; any
other change since — in another tab, say — refuses the order as a whole: "Your order has changed since you reviewed it.
Please check it and place it again." Either way the review shows today's order.

**Type:** Domain failure (`DomainException`) · **Related terms:** Order fingerprint, `CheckoutSession`

---

### CheckoutSessionNotFound · CartNotAvailable · CartNotActive · EmptyCart · ArticleNotAvailable · CheckoutItemsUnavailable

**Definition:** What the use case could not get hold of before a session exists, or could not address afterwards.
Session and cart lookups are scoped to the caller, so "not yours" reads as "not found" (ADR-007).
`CheckoutItemsUnavailable` is the start's refusal of a cart whose lines are unavailable or short of stock; it
carries their product ids, in cart order.

**Type:** Use-case failure (`UseCaseException`) · **Related terms:** `ICartDataPort`, `ICheckoutArticleDataPort`

---

### PaymentProviderNotFound · PaymentProviderUnavailable · PaymentInitiationFailed

**Definition:** The selected provider is unknown to the shop; it is unavailable (it says it is not taking payments,
cannot be reached, does not answer in time or answers outside its contract); or it refused to open a payment. The
provider's reason travels on the failure and into the log. The customer sees the shop's own text: "The payment was
refused. Please choose another way to pay." for a refusal, "The payment provider is not available right now. Please
try again later." when the provider is unavailable — at the payment step or, for Place Order, on the review.

**Type:** Use-case failure (`UseCaseException`) · **Related terms:** `IPaymentProviderRegistry`, `IPaymentProvider`,
`PaymentResult`

---

### MailProviderUnavailable (MailProviderUnavailableException)

**Definition:** The mail provider did not accept a mail: it answered other than `202`, did not answer in time or
could not be reached. The failure carries the mail's key; the delivery that asked for the mail is tried again later,
under the same key. No customer sees it.

**Type:** Use-case failure (`UseCaseException`) · **Related terms:** `IOrderConfirmationMailer`, Order confirmation, Mail outcome

---

### BuyerInfoNotSubmitted (BuyerInfoNotSubmittedException)

**Definition:** The order confirmation was asked for a checkout session that has no buyer info: its first step was
never submitted. A confirmed checkout always has one, so only a session that was never confirmed reaches this. The
failure carries the session id; the log says it by name, no customer sees it.

**Type:** Use-case failure (`UseCaseException`) · **Related terms:** `BuyerInfo`, Order confirmation, `CheckoutNotConfirmed`

---

### ShippingOptionNotOffered (ShippingOptionNotOfferedException)

**Definition:** The shipping option the delivery form names is not one the delivery step offers; the step is refused
and asks for another choice. The failure carries the refused identifier. The customer sees "This shipping method is
not offered. Please choose another one."

**Type:** Use-case failure (`UseCaseException`) · **Related terms:** `ShippingOption`, `ShippingOptions`

---

### PaymentResult

**Definition:** What the payment provider made of a payment operation, as checkout understands it. A payment
request carries the checkout session's total amount and currency and the checkout reference to the provider;
its outcome is one of:

- `Succeeded` — an **authorized payment**: the provider accepted it and gave a payment reference. Only then
  does Place Order confirm the session, which records the authorized `PaymentSelection` and publishes
  `PaymentSubmitted`.
- `Refused` — a **refused payment**: the provider answered and declined. The checkout stays at the review, no
  order is placed and the reserved stock is released (`PaymentInitiationFailed`).
- `Unavailable` — an **unavailable provider**: no answer within 2 seconds, no connection, or an answer outside
  the provider's contract. The checkout stays at the review, as for a refusal (`PaymentProviderUnavailable`).

**Type:** Concept (result of the `IPaymentProvider` output port)

**Related terms:** `PaymentProviderId`, `PaymentSelection`, `PaymentSubmitted`.

**Notes:** The **payment stand-in** (`MockPaymentProvider`, id `mock`) authorizes every payment and takes
payments where no provider address is configured. The provider's contract has no confirmation: the REST adapter
answers it with `Succeeded` without asking the provider. A cancellation is asked of the provider (see Payment
cancellation).

### Payment cancellation (CancelPayment)

**Definition:** Releases an authorized payment at the payment provider by its payment reference, so the provider
no longer holds it for the checkout. The REST adapter sends `POST /payments/{payment reference}/cancel`; `204`
cancels the payment, any other answer, no answer in time or no connection counts as `Unavailable`. No checkout step
asks for a cancellation: Place Order checks the session and the stock before it requests the payment, and when the
confirmation fails after the authorization, the reserved stock is released and no cancellation is sent.

**Type:** Concept (operation `CancelPaymentAsync` of the `IPaymentProvider` output port)

**Related terms:** `PaymentResult`, `PaymentSelection`, Authorized amount.

### Checkout reference (merchantReference)

**Definition:** The reference the shop sends with every payment request of a checkout: the checkout session's id
as text, the same in every request of that checkout, so a repeated request names the same checkout and the
provider's answer for one checkout never touches another. It is not the payment reference the provider answers
with (`PaymentSelection`), and not the order reference.

**Type:** Concept (field `merchantReference` of the provider's payment request, formed in `RestPaymentProvider`)

**Related terms:** `CheckoutSessionId`, `PaymentSelection`, `PaymentResult`.

## Features

The parts of the context's behaviour its people name; each groups what holds in a folder of the living spec.

### Payment (payment)

**Definition:** Paying for the checkout's order through a payment provider — authorized, refused or unanswered.

**Type:** Feature

### Checkout session (session)

**Definition:** Starting the checkout from the cart; nobody reaches another visitor's checkout.

**Type:** Feature

### Buyer details (buyerdetails)

**Definition:** Who is buying — entered by a guest, known for a signed-in customer.

**Type:** Feature

### Delivery (delivery)

**Definition:** The delivery address and the shipping option.

**Type:** Feature

### Ordering (ordering)

**Definition:** The review, placing the order and the confirmation — checked against what can still be sold.

**Type:** Feature

### Checkout steps (steps)

**Definition:** The steps' progress and order, going back to an earlier step, and the way to the cart without an open checkout.

**Type:** Feature

### Cart sync (cartsync)

**Definition:** The open checkout keeps its order while the cart changes.

**Type:** Feature

### Order confirmation (orderconfirmation)

**Definition:** The mail telling the buyer the order went through: sent to the buyer e-mail of the confirmed
checkout, with the order total, once per order. The order is the confirmed checkout session; its key at the mail
provider is `order-confirmation-<checkout session id>`, the same for every attempt. A mail provider that does not
accept the mail delays it — the delivery is tried again later — and never undoes the order.

**Type:** Feature

**Operations:** send the order confirmation (`sendorderconfirmation`) — mails a confirmed checkout's total to its
buyer e-mail, once per order (`SendOrderConfirmation`, run by `CheckoutConfirmedEventConsumer` after
`CheckoutConfirmed` is committed).

**Related terms:** `CheckoutConfirmed`, `BuyerInfo`, `CheckoutTotals`, `MailProviderUnavailable`, `BuyerInfoNotSubmitted`.

### Mail provider

**Definition:** The outside service that sends the shop's mails; it accepts a mail under a key and sends one
mail per key. Behind the output port `IOrderConfirmationMailer`; with no provider address configured, a stand-in
accepts every mail and sends nothing.

**Type:** Concept

**Related terms:** Order confirmation, Mail outcome, `MailProviderUnavailable`.

### Mail outcome (MailOutcome)

**Definition:** The mail provider's answer to a mail, as checkout understands it — one of:

- `Accepted` — the provider took the mail for sending (it answered `202`).
- `Unavailable` — no usable answer: the provider answered other than `202`, did not answer in time or could not be
  reached. The mail is asked for again later under the same key (`MailProviderUnavailable`).

**Type:** Concept (answer of the `IOrderConfirmationMailer` output port)

**Related terms:** Mail provider, Order confirmation, `MailProviderUnavailable`, `PaymentResult`.
