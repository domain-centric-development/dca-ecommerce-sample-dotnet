---
id: early-step-leads-to-the-open-step
epic: guest-checkout
status: delivered
delivered: 2026-10-13T16:29:01Z
delivered_by: gate
context: checkout
feature: steps
title: A step sent too early leads to the step that is still open
depends_on: [place-the-order-from-the-review-and-see]
---

# A step sent too early leads to the step that is still open

## Story

As a guest who sends a checkout step before the earlier ones are done I want to be taken to the step that is
still open, so that I see at once what is missing.

## Acceptance criteria

### Rule: A step's form sent before the earlier steps are done changes nothing

#### delivery-sent-before-the-buyer-step (happy path)
- Given a guest who has just started the checkout and has not submitted the buyer step
- When a delivery form with Street Address "Hauptstrasse 1", City "Berlin", Postal Code "10115", Country
  "Germany" and "Standard Shipping" is sent to the shop
- Then the guest ends on the buyer step at `/checkout/buyer`
- And the review cannot be opened afterwards: `/checkout/review` opens the buyer step

#### place-order-before-the-review
- Given a guest who has submitted the buyer and delivery steps and is on the payment step
- When a "Place Order" is sent to the shop
- Then the guest ends on the payment step at `/checkout/payment`
- And no confirmation is shown: the order is not placed

## Changed expectations

- A delivery form sent before the buyer step is done re-renders the delivery page with "Please complete the
  previous checkout steps first." now, and ends on the buyer step at `/checkout/buyer` after.
- Place Order sent before the review re-renders the review page now, and ends on the payment step at
  `/checkout/payment` after; nothing is placed, before and after.

## Notes

- The shop in the other language already behaves like this; this story closes the gap so that both hold the same spec
  (WP-107, decision `the-steps-show-the-progress-01`).
