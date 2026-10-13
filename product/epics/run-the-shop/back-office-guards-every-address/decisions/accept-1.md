---
id: back-office-guards-every-address-accept-1
story: back-office-guards-every-address
stage: document
kind: acceptance
asked: 2026-10-13T01:32:00Z
digest: 049c53fda21736c6de8a86c8c92966675cc39705647a23d96d230ec325c3dd20
---

# Accept back-office-guards-every-address?

## Question
Every gate passed. Look at what the story delivers before it counts as delivered:

- login-after-a-redirect-opens-the-event-log: Given a browser that opened `/backoffice/settings` and was sent to the backoffice login page; When the operator logs in with username "admin" and password "admin"; Then the event log `/backoffice/events` opens — `DcaShop.E2eTests.BackofficeE2eTest#LoginAfterARedirectOpensTheEventLog`

Start the application with `dotnet run --project src/DcaShop.Web`.

## Options
- accepted: the story is delivered.
- a correction: what should be different, written into the story (criteria and an `answered:` line naming this record); the story runs again from plan.
