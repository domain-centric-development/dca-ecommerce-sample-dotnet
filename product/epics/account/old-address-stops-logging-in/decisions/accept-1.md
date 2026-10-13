---
id: old-address-stops-logging-in-accept-1
story: old-address-stops-logging-in
stage: document
kind: acceptance
asked: 2026-10-13T06:47:39Z
digest: 270faddba30f67efb9718142e408e71b7d594a0702c9de10f16015f9968d82f2
---

# Accept old-address-stops-logging-in?

## Question
Every gate passed. Look at what the story delivers before it counts as delivered:

- old-address-no-longer-logs-in: Given the customer changed the address from "anna.schmidt@example.com" to "anna.meyer@example.com" and a browser with no cookies; When they log in with "anna.schmidt@example.com" and "Secret123"; Then the login page shows "Invalid email or password" — `DcaShop.E2eTests.ProfileE2eTest#AnAddressGivenUpOnTheProfileIsRefusedAtTheLogin`

Start the application with `dotnet run --project src/DcaShop.Web`.

## Options
- accepted: the story is delivered.
- a correction: what should be different, written into the story (criteria and an `answered:` line naming this record); the story runs again from plan.

## Answer
answer: accepted
by: Christoph Bloemer
at: 2026-10-13T07:30:03Z
