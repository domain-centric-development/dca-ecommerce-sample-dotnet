---
id: event-log-has-an-entry-per-listener-accept-1
story: event-log-has-an-entry-per-listener
stage: document
kind: acceptance
asked: 2026-10-13T16:26:55Z
digest: 7658b96db4cb6edaad692ab889db7c32c99a750af15ed726ae15582c71314fb9
---

# Accept event-log-has-an-entry-per-listener?

## Question
Every gate passed. Look at what the story delivers before it counts as delivered:

- failed-entry-offers-a-retry: Given the operator is logged in and a delivery has used up its five attempts ("Status: FAILED, attempts: 5"); When they open `/backoffice/events`; Then that entry shows the button "Retry failed listener" — `DcaShop.E2eTests.BackofficeRetryE2eTest#AnEntryWhoseDeliveryUsedUpItsAttemptsOffersTheRetryOfItsListener`
- retry-leaves-other-listeners-alone: Given an event with two listeners, one delivery "Completed" and the other failed for good, and the operator logged in; When the operator presses "Retry failed listener" on the failed entry; Then the other listener's entry keeps its "Completed" date unchanged — `DcaShop.IntegrationTests.BackofficeListenerRetryTest#RetryingTheFailedListenerLeavesTheOtherListenersCompletionDateAsItWas, DcaShop.IntegrationTests.BackofficeEventLogPageTest#ARetryWithoutItsListenerIsRefusedAsMalformed`

Start the application with `dotnet run --project src/DcaShop.Web`.

## Options
- accepted: the story is delivered.
- a correction: what should be different, written into the story (criteria and an `answered:` line naming this record); the story runs again from plan.

## Answer
answer: accepted
by: Christoph Blömer
at: 2026-10-13T17:04:00Z
