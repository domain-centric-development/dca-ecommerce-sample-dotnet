---
id: event-log-has-an-entry-per-listener
epic: run-the-shop
status: delivered
delivered: 2026-10-13T17:12:34Z
delivered_by: gate
created: 2026-10-13T19:17:38Z
context: backoffice
feature: redelivery
title: The event log has an entry per listener, each with its own retry
depends_on: [event-log-marks-incomplete-deliveries]
---

# The event log has an entry per listener, each with its own retry

## Story

As the operator I want one entry per event and listener, each with its own state and its own retry, so that I
retry exactly the delivery that failed and see the others untouched.

## Acceptance criteria

### Rule: A failed entry offers a retry

#### failed-entry-offers-a-retry (happy path)
- Given the operator is logged in and a delivery has used up its five attempts ("Status: FAILED, attempts: 5")
- When they open `/backoffice/events`
- Then that entry shows the button "Retry failed listener"

### Rule: A retry delivers the event once more to that one listener

#### retry-leaves-other-listeners-alone
- Given an event with two listeners, one delivery "Completed" and the other failed for good, and the operator logged in
- When the operator presses "Retry failed listener" on the failed entry
- Then the other listener's entry keeps its "Completed" date unchanged

## Changed expectations

- The log shows one entry per event, its listeners joined into "Listener", now; one entry per event and listener
  after, each with its badge, its "Completed" date and, when it failed, its own retry.
- The retry button reads "Retry failed consumers" now and "Retry failed listener" after, with a `data-test`.

## Notes

- The shop in the other language already behaves like this; this story closes the gap so that both hold the same spec
  (WP-107, decision `retry-a-failed-delivery-01`).
