---
id: event-payload-shows-status-and-attempts
epic: run-the-shop
status: delivered
delivered: 2026-10-13T07:39:46Z
delivered_by: gate
context: backoffice
feature: eventlog
title: An event's payload opens with its status and every attempt
depends_on: [operator-login-to-the-back-office]
---

# An event's payload opens with its status and every attempt

## Story

As the operator I want an event's payload to say first how its delivery stands and how often it was tried,
so that I read a delivery's history before its content.

## Acceptance criteria

### Rule: The payload starts with a line "Status: <status>, attempts: <number>", followed by the event's content

#### payload-starts-with-status-and-attempts (happy path)
- Given the operator is logged in and the log lists a "Completed" entry for a product created at start-up, delivered at the first attempt
- When they expand that entry's "Event Payload"
- Then its first line reads "Status: COMPLETED, attempts: 1"
- And the lines after it hold the event's content: eventId, occurredOn, productId, amount, currency and initialStock

## Changed expectations

- The payload's first line reads `Status: Completed` now — the status's name in mixed case, no attempts — and
  `Status: COMPLETED, attempts: <every try, the successful one included>` after; the JSON and the progress line follow
  as they do.

## Notes

- The shop in the other language already behaves like this; this story closes the gap so that both hold the same spec
  (WP-107, decision `look-into-an-events-payload-01`).
