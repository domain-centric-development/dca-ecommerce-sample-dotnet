---
id: the-event-log-lists-every-delivery
epic: run-the-shop
status: delivered
delivered: 2026-10-13T01:45:36Z
delivered_by: gate
created: 2026-10-12T20:20:16Z
context: backoffice
feature: eventlog
title: The event log lists every delivery
depends_on: [operator-login-to-the-back-office, CAT-01, event-log-marks-incomplete-deliveries]
---

# The event log lists every delivery

## Story

As an operator I want to see every event the shop published and whether each of its listeners has handled
it, newest first, so that I notice a delivery that did not go through before a customer does.

## Acceptance criteria

<!-- happy-path: log-lists-the-events-published-at-start-up -->

### Rule: The shop's start-up already shows in the log

#### log-lists-the-events-published-at-start-up
- Given the shop has started and seeded the sample catalogue, and the operator is logged in
- When they open `/backoffice/events`
- Then the log lists at least one entry
- And every entry shows a type label that names the event

### Rule: One entry per event and listener, with its state and dates

#### entry-shows-listener-state-and-dates
- Given the operator is logged in and the log lists the start-up events
- When they open `/backoffice/events`
- Then every entry shows a badge "Completed" or "Incomplete", a "Listener" and a "Published" date in the form yyyy-MM-dd HH:mm:ss
- And every entry with the badge "Completed" also shows a "Completed" date in the same form

#### newest-entry-comes-first
- Given the operator is logged in and the log lists the start-up events
- When they open `/backoffice/events`
- Then the entries are ordered by their "Published" date, the newest first

### Rule: The summary counts the entries

#### counts-add-up
- Given the operator is logged in and the log lists the start-up events
- When they open `/backoffice/events`
- Then "Total Events" equals the number of entries listed
- And "Completed" is the number of entries with the badge "Completed", "Incomplete" the number with the badge "Incomplete", and both add up to "Total Events"

### Rule: The page is recognisable and can be reloaded

#### log-page-breadcrumb
- Given the operator is logged in
- When they open `/backoffice/events`
- Then the breadcrumb reads "Home / Event Publication Log" and "Home" links to `/`

#### refresh-reloads-the-log
- Given the operator is logged in on `/backoffice/events`
- When they press "Refresh"
- Then `/backoffice/events` is loaded again with its summary and entries

## Out of scope

- The payload of an entry — `look-into-an-events-payload`.
- Retrying a failed delivery — `failed-deliveries-are-retried-on-their` (automatic) and `retry-a-failed-delivery` (by the operator).
- The "Event Log" link in the shop footer — the page frame, `browse-catalogue`.
- An empty log ("No events published yet"): the shop always publishes events at start-up, so the empty log is
  not reachable from outside.

## Notes

- Replay story: `OPS-02` (run-the-shop).
- Shared scenario: `scenario.backoffice.event-log-counts`.
- Inventory: backoffice B5, B6, B9, B12 (breadcrumb).

## Assumptions

- answered: Point 1: b, as one story — the Java shop is right. The .NET shop is brought in line by a change story, event-log-marks-incomplete-deliveries (entry-shows-listener-state-and-dates and counts-add-up as written: the badge "Incomplete" for every entry that has not completed), which this story and failed-deliveries-are-retried-on-their and retry-a-failed-delivery depend on. This story stays adopted as written. Point 2: a — list tests/DcaShop.E2eTests/BackofficeE2eTest.cs under ## Changed tests; EventLogShowsPublishedEvents keeps its title and gains "every entry has a type label". (the-event-log-lists-every-delivery-01)
