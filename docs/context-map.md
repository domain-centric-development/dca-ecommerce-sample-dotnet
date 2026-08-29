# DcaShop Context Map

> **Generated file — do not edit.** Derived from the `[BoundedContext]`, `[Upstream]`,
> `[ExternalUpstream]`, and `[Partnership]` context attributes by
> `ContextMapRenderer`. After changing a declaration, regenerate and commit this file.

Each side declares only what it controls: the downstream declares its consumed upstreams
(`[Upstream]`: translation strategy and channel), the upstream publishes its contract
(`api`/`events` namespaces, `[OpenHostService]`), and partnerships are declared
symmetrically on both contexts. Organizational patterns such as Customer–Supplier are not
machine-classified; Separate Ways is the absence of any declaration. External systems
appear via `[ExternalUpstream]` on their consuming context — the model dependency always
points to the external system, regardless of who initiates the exchange. Non-context
modules and the shared kernel are intentionally not part of this map.

## Bounded Contexts

| Module | Name | Description | Published interfaces |
|---|---|---|---|
| Cart | Shopping Cart | Cart management, item additions/removals, and cart lifecycle | api, events |
| Checkout | Checkout | Checkout process, order placement, and payment orchestration | events |
| Product | Product Catalog | Product management and catalog browsing | api, events |

## Diagram

```mermaid
graph LR
  Cart["Shopping Cart<br/><i>api · events</i>"]
  Checkout["Checkout<br/><i>events</i>"]
  Product["Product Catalog<br/><i>api · events</i>"]

  Cart -->|"ACL / api"| Product
  Checkout -->|"ACL / api"| Product
  Checkout -->|"ACL / api"| Cart
  Checkout -.->|"Conformist / events"| Cart
  ext_payment_service_provider[["Payment Service Provider"]]
  Checkout -->|"ACL / REST"| ext_payment_service_provider
  Cart ---|"Partnership"| Checkout
```

Arrows point from downstream to upstream (dependency direction, never call direction).
Solid arrows are synchronous consumption (`api` / external `outbound`), dotted arrows are
asynchronous consumption (`events` / external `inbound`), plain lines are partnerships.
Double-framed nodes are external systems. Node badges list published interfaces.
Edges labeled `planned` are declared intent without a code dependency yet.

## Upstream relationships

| Downstream | Upstream | Channel | Translation | Status | Rationale |
|---|---|---|---|---|---|
| Cart | Product | api | ACL | implemented | Cart works with its own article snapshot; the catalog model must not leak into cart invariants |
| Checkout | Product | api | ACL | implemented | Product data is translated into checkout's own article types |
| Checkout | Cart | api | ACL | implemented | Cart snapshots are translated into checkout's own CartData |
| Checkout | Cart | events | Conformist | implemented | CheckoutConfirmedEvent implements cart's consumer-defined ICartCompletionTrigger contract as-is |

## External systems

| Consumer | External system | Interaction | Protocol | Exchanges | Translation | Status | Rationale |
|---|---|---|---|---|---|---|---|
| Checkout | Payment Service Provider | outbound | REST | payment operations (initiate, confirm, refund) | ACL | implemented | Behind the caller-owned IPaymentProviderRegistry port; the sample ships an in-memory registry in place of a real gateway |

## Partnerships

| Contexts | Rationale |
|---|---|
| Cart ↔ Checkout | Cart owns the consumer-defined ICartCompletionTrigger contract that checkout events implement; both contexts evolve it together — Checkout implements cart's consumer-defined ICartCompletionTrigger contract; both contexts evolve it together |
