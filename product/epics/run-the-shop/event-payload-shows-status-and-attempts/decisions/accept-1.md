---
id: event-payload-shows-status-and-attempts-accept-1
story: event-payload-shows-status-and-attempts
stage: document
kind: acceptance
asked: 2026-10-13T07:09:36Z
digest: 12a098a26843ba91348e752735f27a2d640c1cbd2dc9b3eb15c1319524415d5f
---

# Accept event-payload-shows-status-and-attempts?

## Question
Every gate passed. Look at what the story delivers before it counts as delivered:

- payload-starts-with-status-and-attempts: Given the operator is logged in and the log lists a "Completed" entry for a product created at start-up, delivered at the first attempt; When they expand that entry's "Event Payload"; Then its first line reads "Status: COMPLETED, attempts: 1"; And the lines after it hold the event's content: eventId, occurredOn, productId, amount, currency and initialStock — `DcaShop.E2eTests.BackofficeE2eTest#PayloadOpensWithItsStatusAndAttemptsBeforeTheEventsContent`

Start the application with `dotnet run --project src/DcaShop.Web`.

## Options
- accepted: the story is delivered.
- a correction: what should be different, written into the story (criteria and an `answered:` line naming this record); the story runs again from plan.

## Answer
answer: accepted
by: Christoph Bloemer
at: 2026-10-13T07:30:03Z
