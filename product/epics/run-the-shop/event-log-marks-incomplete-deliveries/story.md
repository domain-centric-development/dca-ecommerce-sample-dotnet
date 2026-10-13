---
id: event-log-marks-incomplete-deliveries
epic: run-the-shop
status: delivered
delivered: 2026-10-13T01:33:55Z
delivered_by: gate
context: backoffice
feature: eventlog
title: The event log marks an incomplete delivery as Incomplete
depends_on: [operator-login-to-the-back-office]
---

# The event log marks an incomplete delivery as Incomplete

## Story

As the operator I want every delivery that has not completed marked the same way, so that I see at a glance what
still waits, whatever its state.

## Acceptance criteria

### Rule: One entry per event and listener, with its state and dates

#### entry-shows-listener-state-and-dates (happy path)
- Given the operator is logged in and the log lists the start-up events
- When they open `/backoffice/events`
- Then every entry shows a badge "Completed" or "Incomplete", a "Listener" and a "Published" date in the form yyyy-MM-dd HH:mm:ss
- And every entry with the badge "Completed" also shows a "Completed" date in the same form

### Rule: The summary counts the entries

#### counts-add-up
- Given the operator is logged in and the log lists the start-up events
- When they open `/backoffice/events`
- Then "Total Events" equals the number of entries listed
- And "Completed" is the number of entries with the badge "Completed", "Incomplete" the number with the badge "Incomplete", and both add up to "Total Events"

## Changed expectations

- An entry that has not completed shows its publication status as its badge ("Staged", "Pending", "Failed") now,
  and the badge "Incomplete" after; the retry button stays where it is.

## Notes

- The shop in the other language already behaves like this; this story closes the gap so that both hold the same spec
  (WP-107, decision `the-event-log-lists-every-delivery-01`).
