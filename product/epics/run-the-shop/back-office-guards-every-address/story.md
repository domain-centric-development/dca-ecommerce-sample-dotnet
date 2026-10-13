---
id: back-office-guards-every-address
epic: run-the-shop
status: approved
context: backoffice
feature: access
title: The back office guards every address below it
depends_on: [operator-login-to-the-back-office]
---

# The back office guards every address below it

## Story

As the operator I want every address below the back office to ask for the operator's login and to bring me to the
event log once I am in, so that no back-office address is open or a dead end.

## Acceptance criteria

### Rule: Every back-office page needs an operator login

#### login-after-a-redirect-opens-the-event-log (happy path)
- Given a browser that opened `/backoffice/settings` and was sent to the backoffice login page
- When the operator logs in with username "admin" and password "admin"
- Then the event log `/backoffice/events` opens

## Changed expectations

- An address below `/backoffice` that names no page (`/backoffice/settings`) shows the not-found page now, and sends a
  visitor without the operator's login to the backoffice login page after.

## Notes

- The shop in the other language already behaves like this; this story closes the gap so that both hold the same spec
  (WP-107, decision `the-back-office-stays-closed-to-others-01`).
