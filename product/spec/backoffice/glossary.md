# Backoffice — Ubiquitous Language (Bootstrap)

> **Bootstrap status:** This glossary was initially derived from the existing code
> (`Application/`, `Adapter/`). Backoffice is a **Bounded Context of a generic
> subdomain** — operating this application — and has no `Domain/` model of its own:
> in transaction-script style the use case reads a Store and maps to a Result, which
> the pattern-selection decision (ADR-003) allows for a generic subdomain. Terms are
> therefore taken from the application and adapter layers.

## Context Character

Backoffice is the Bounded Context for **operating this application**: monitoring,
the event publication log, future dashboards and operator navigation. Its language
is its own — an *event publication* is a dispatched event with a completion status,
a term no business context uses. Context-specific admin pages (products, prices,
inventory) belong to their own Bounded Contexts under `/backoffice/{context}/`, not
here.

## Concepts

### EventPublicationLog

**Definition:** Logical, append-only audit log of everything the shop has published,
including its delivery status. Backoffice reads this log; the writes belong to
the integration-event outbox, which registers a publication inside the publishing use
case's transaction and marks it completed once it has been delivered.

**Type:** Concept (operational data set, not an Aggregate)

**Related terms:** `EventPublicationLogStore`, `EventPublicationEntry`

**Notes:** The log has no Aggregate lifecycle of its own — it is therefore
accessed via a **Store** (not via a Repository).

---

### EventPublicationEntry

**Definition:** A single entry from the event publication log: one delivery of a
published event to one listener, with that listener's own status, publication and
completion timestamps and a serialized payload. An event with two listeners has two
entries.

**Type:** Value Object (read schema of the Store)

**Key:** `Guid Id` (technical publication id, shared by the entries of one
event's listeners) together with `ListenerId` — a technical key, not a business identifier

**Related terms:** `EventPublicationLogStore`, `EventPublicationSummary`

**Operations:**
- `IsCompleted` — has the delivery to this listener been made?
- `ShortEventType` — type name of the event without its namespace
- Retry failed listener (`ReplayFailed`) — delivers a failed entry's event once more
  to that entry's listener alone; the other listeners' entries stay as they were
  (use case `ReplayFailedPublication`)

**Notes:** Belongs to the Store as its **read schema** and is deliberately
decoupled from the use-case API (see `EventPublicationSummary`). The listener is
what the integration-event outbox calls a *consumer*; the outgoing adapters
translate between the two words.

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
both what is still queued and what failed and is being retried. The event log's
badge shows exactly these two words; a **failed** publication is incomplete and is
the one an operator can retry from the log.

**Type:** Concept (derived attribute)

**Related terms:** `EventPublicationEntry.IsCompleted`,
`GetEventPublicationsResult.CompletedCount` / `IncompleteCount`, `Publication Status`

---

### Publication Status

**Definition:** The outbox's own status of a delivery to one listener — `STAGED`,
`PENDING`, `FAILED` or `COMPLETED`. An entry's payload names it upper-case on its first line,
`Status: <STATUS>, attempts: <number>`. The badge reduces it to the two words of
the **Completion Status**: `COMPLETED` is completed, every other status is incomplete.

**Type:** Concept

**Related terms:** `Completion Status`, `Attempt`

---

### Attempt

**Definition:** One try at delivering an event to one listener, counted per entry.
The number on the payload's first line counts every try, including the successful
one. A delivery made at its first try reads `attempts: 1`; a delivery that used up
its attempts reads `Status: FAILED, attempts: 5`.

**Type:** Concept

**Related terms:** `Publication Status`, `EventPublicationEntry`

**Notes:** The outbox records failed deliveries only, so a pending or failed delivery
counts only its failed tries. The store adds the successful try once the listener's
delivery is completed (`OutboxEventPublicationLogStore.Tries`).

---

### Failed (`Failed`)

**Definition:** A delivery whose attempts are used up: Publication Status `Failed` with no
automatic retry left (attempts at or above `EventRetryPolicy.MaxAttempts`). Only a failed
entry offers "Retry failed listener" in the event log; an entry whose delivery is still under
way, or that waits for its automatic retry, has not failed and offers none.

**Type:** Concept (derived attribute)

**Related terms:** `Publication Status`, `Attempt`, `EventPublicationPageViewModel.Failed`

**Notes:** The outbox sets the Publication Status `Failed` only once the attempts are used up;
while attempts are left the delivery stays `Pending` with its next attempt scheduled
(`InMemoryEventOutbox.RecordConsumerFailure`).

---

### Operator

**Definition:** The person who logs in to the back office (`/backoffice/login`) to run the
shop — not a shopper. A back-office page's header is the operator's: the shop's name,
"Back office" and the operator's Logout, nothing of a shopper logged in to the shop in
the same browser.

**Type:** Concept (actor)

**Related terms:** `BackofficeOptions` (the operator's credentials and session),
`Views/Backoffice/_BackofficeLayout.cshtml` (the operator's actions in the header)

**Synonyms (avoid):** "admin" — the name of the development operator account, not the role.

---

## Store / Repository — Distinction

**Store** is used in Backoffice instead of **Repository** because the data has
**no Aggregate lifecycle**:

| Aspect          | Repository                                      | Store (here)                                    |
|-----------------|-------------------------------------------------|-------------------------------------------------|
| Lifecycle       | Load → mutate → save                            | Append-only, written externally                 |
| Content         | Aggregate Roots                                 | Operational entries without business identity   |
| Operations      | `FindByIdAsync`, `SaveAsync`, `DeleteByIdAsync` | `FindAllAsync`, count, record (no save)         |
| Example         | `IShoppingCartRepository`                       | `IEventPublicationLogStore`                     |

Backoffice does **not** write to the log — the outbox owns the
publications.
`OutboxEventPublicationLogStore` is **read-only**.

---

## Ports & Use Cases

### EventPublicationLogStore (Output Port)

**Definition:** Read port onto the shop's event publication log.

**Type:** Output Port (`IStore` marker)

**Operations:** `FindAllAsync(): IReadOnlyList<EventPublicationEntry>`

**Notes:** Implementation: `OutboxEventPublicationLogStore` (reads the
integration-event outbox). One row per **integration** event and listener, not per
domain event and listener: this shop dispatches domain events in-process and keeps
no registry of them. Before an event's listeners are known, it has one row under
the outbox's placeholder listener.

---

### GetEventPublications (Use Case)

**Definition:** Returns all event publications with delivery status and
aggregated statistics (`TotalCount`, `CompletedCount`, `IncompleteCount`, each
counting entries — deliveries to one listener) for the Backoffice overview page.

**Type:** Use Case (Query)

**Related terms:** `GetEventPublicationsQuery`, `GetEventPublicationsResult`,
`EventPublicationSummary`

**Notes:** Query is currently parameterless. Possible extensions: filter by
event type, time range, completion status.

---

## Features

The parts of the context's behaviour its people name; each groups what holds in a folder of the living spec.

### Back-office access (access)

**Definition:** The operator's login; the back office closed to everyone else.

**Type:** Feature

### Event log (eventlog)

**Definition:** Every delivery of an event, listed, with its payload.

**Type:** Feature

### Redelivery (redelivery)

**Definition:** Failed deliveries retried on their own, and by an operator.

**Type:** Feature

## Open Questions

- Should the term **Event Publication** be promoted to the Shared Kernel
  glossary? (It will likely be relevant for other operational views as well.)
- Should Backoffice become its own Bounded Context in the long term, or remain
  classified as a Generic Subdomain?
