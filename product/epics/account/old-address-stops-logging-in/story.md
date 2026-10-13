---
id: old-address-stops-logging-in
epic: account
status: approved
context: account
feature: profile
title: A changed address takes the old one out of the login
depends_on: [log-in-and-log-out]
---

# A changed address takes the old one out of the login

## Story

As a customer who changed my address I want the old one to stop working for the login, so that an address I
gave up gives nobody access to my account.

## Acceptance criteria

### Rule: The new address is the login from now on

#### old-address-no-longer-logs-in (happy path)
- Given the customer changed the address from "anna.schmidt@example.com" to "anna.meyer@example.com" and a browser with no cookies
- When they log in with "anna.schmidt@example.com" and "Secret123"
- Then the login page shows "Invalid email or password"

## Changed expectations

- After a change of address, logging in with the old address still succeeds now and is refused with "Invalid email
  or password" after (the in-memory account store keeps the old address in its index).

## Notes

- The shop in the other language already behaves like this; this story closes the gap so that both hold the same spec
  (WP-107, decision `keep-the-profile-up-to-date-01`).
