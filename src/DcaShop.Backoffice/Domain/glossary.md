# Backoffice — Ubiquitous Language (Bootstrap)

> **Bootstrap status:** This glossary was initially derived from the existing code
> (`Application/`, `Adapter/`). Backoffice currently has no `Domain/` model of its
> own — it carries no `[BoundedContext]` marker because it is an **operational
> module**, not a business Bounded Context. Terms are taken from the application and
> adapter layers. Please review and extend from a business perspective.

## Module Character

Backoffice is an **operational cross-cutting module** for administrative views
(monitoring, event log, future dashboards/admin navigation). Context-specific
admin pages (products, prices, inventory) live in their respective Bounded
Contexts under `/backoffice/{context}/`, not here.

## Concepts

### EventPublicationLog

**Definition:** Logical, append-only audit log of everything the shop has published,
including its delivery status. Backoffice reads this log; the writes belong to the
integration-event outbox, which registers a publication inside the publishing use
case's transaction and marks it completed once it has been delivered.

**Type:** Concept (operational data set, not an Aggregate)

**Related terms:** `EventPublicationLogStore`, `EventPublicationEntry`

**Notes:** The log has no Aggregate lifecycle of its own — it is therefore
accessed via a **Store** (not via a Repository).

---

### EventPublicationEntry

**Definition:** A single entry from the event publication log: an event
published to a specific listener, with publication and completion timestamps
and a serialized payload.

**Type:** Value Object (read schema of the Store)

**Identity:** `Guid Id` (technical publication id, not a business identifier)

**Related terms:** `EventPublicationLogStore`, `EventPublicationSummary`

**Operations:**
- `IsCompleted` — has the publication been delivered?
- `ShortEventType` — type name of the event without its namespace

**Notes:** Belongs to the Store as its **read schema** and is deliberately
decoupled from the use-case API (see `EventPublicationSummary`).

---

### EventPublicationSummary

**Definition:** Application-layer view of an event publication entry. Exposes
exactly the fields the view needs and decouples the use-case API from the
Store's read schema.

**Type:** Value Object (use-case result payload)

**Identity:** `Guid Id`

**Synonyms (avoid):** "EventPublication", "event entry" — please consistently
distinguish `EventPublicationSummary` (application layer) from
`EventPublicationEntry` (store layer).

**Related terms:** `EventPublicationEntry`, `GetEventPublicationsResult`

**Operations:**
- `From(EventPublicationEntry)` — mapping from the Store entry
- `IsCompleted`, `ShortEventType`

**Notes:** **Clarification Entry vs. Summary:**
- `EventPublicationEntry` = persistence-near representation returned by the
  Store (owned by the port `EventPublicationLogStore`).
- `EventPublicationSummary` = view-oriented representation returned by the
  use case (owned by the use case). Intentional duplication so the two
  contracts can evolve independently.

---

### Completion Status

**Definition:** Status of an event publication: **completed** when it has been
delivered (`CompletionDate is not null`), otherwise **incomplete** — which covers
both what is still queued and what failed and is being retried.

**Type:** Concept (derived attribute)

**Related terms:** `EventPublicationEntry.IsCompleted`,
`GetEventPublicationsResult.CompletedCount` / `IncompleteCount`

---

## Store / Repository — Distinction

**Store** is used in Backoffice instead of **Repository** because the data has
**no Aggregate lifecycle**:

| Aspect          | Repository                                | Store (here)                                    |
|-----------------|-------------------------------------------|-------------------------------------------------|
| Lifecycle       | Load → mutate → save                      | Append-only, written externally                 |
| Content         | Aggregate Roots                           | Operational entries without business identity   |
| Operations      | `FindByIdAsync`, `SaveAsync`, `DeleteByIdAsync` | `FindAllAsync`, count, record (no save)   |
| Example         | `IOrderRepository`                        | `IEventPublicationLogStore`                     |

Backoffice does **not** write to the log — the outbox owns the publications.
`OutboxEventPublicationLogStore` is **read-only**.

---

## Ports & Use Cases

### EventPublicationLogStore (Output Port)

**Definition:** Read port onto the shop's publication log.

**Type:** Output Port (`IStore` marker)

**Operations:** `FindAllAsync(): IReadOnlyList<EventPublicationEntry>`

**Notes:** Implementation: `OutboxEventPublicationLogStore` (reads the
integration-event outbox). One row per **integration** event, not one row per
domain event per listener: this shop dispatches domain events in-process and keeps
no registry of them. The counts answer the same operator question — how much is
published, how much is through, how much is still owed — while counting different
things.

---

### GetEventPublications (Use Case)

**Definition:** Returns all event publications with delivery status and
aggregated statistics (`TotalCount`, `CompletedCount`, `IncompleteCount`) for
the Backoffice overview page.

**Type:** Use Case (Query)

**Related terms:** `GetEventPublicationsQuery`, `GetEventPublicationsResult`,
`EventPublicationSummary`

**Notes:** Query is currently parameterless. Possible extensions: filter by
event type, time range, completion status.

---

## Open Questions

- Should the term **Event Publication** be promoted to the Shared Kernel
  glossary? (It will likely be relevant for other operational views as well.)
- Planned extensions (retry, republish) — will these live in Backoffice or with
  the outbox dispatcher that owns delivery?
- Should Backoffice become its own Bounded Context in the long term, or remain
  classified as a Generic Subdomain?
