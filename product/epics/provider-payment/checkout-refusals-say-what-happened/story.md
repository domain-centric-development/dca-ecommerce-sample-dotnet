---
id: checkout-refusals-say-what-happened
epic: provider-payment
status: approved
created: 2026-10-23T21:25:20Z
context: checkout
feature: ordering
title: Checkout refusals say what happened
closes: [the-payment-is-authorized-at-place-order#4, an-open-checkout-keeps-its-price#1]
depends_on: [the-take-over-leads-only-to-pages-of-the-shop]
---

# Checkout refusals say what happened

## Story

As a buyer whose step is refused I want the refusal to name what happened, so that I know what to do next.

## Acceptance criteria

### Rule: A refused "Place Order" names what changed since the review

#### a-changed-quantity-is-not-named-a-price-change (happy path)
Title: Place Order after the order changed in another tab names the change, not a price
- Given a customer on the review of "Clean Architecture" once, at 39.99 EUR
- And in another tab the customer changed the cart's quantity of "Clean Architecture" to 2 and went through the checkout to the review again
- When the customer presses "Place Order" on the first tab's review
- Then the review stays open at `/checkout/review` with "Your order has changed since you reviewed it. Please check it and place it again."
- And it does not say that a price changed

### Rule: A way to pay the shop no longer offers sends the customer to choose another

#### a-withdrawn-way-to-pay-opens-the-payment-step
- Given a customer on the review who chose "Mock Payment (Test)"
- And the shop no longer offers "Mock Payment (Test)"
- When the customer presses "Place Order"
- Then the payment step `/checkout/payment` opens with "This way to pay is not offered. Please choose another one."
- And no order is placed

## Changed expectations

- Now: a change of quantity or product since the review is announced as "The price of '…' has changed"; after: it is announced as "Your order has changed since you reviewed it. Please check it and place it again." A price change keeps its own sentence.
- Now: a way to pay withdrawn before "Place Order" answers "Your order could not be placed. Please try again." at the review; after: the payment step opens with "This way to pay is not offered. Please choose another one."

## Out of scope

- A shipping method the shop does not offer: the methods are a fixed list, none can be withdrawn between the delivery step and "Place Order", and the delivery step already refuses an unknown one with "This shipping method is not offered. Please choose another one." Nothing to change.
- A price changed since the review: the shop already says "The price of '…' has changed. Please check your order and place it again." (spec: `ordering`).

## Assumptions

- answered: the words of each refusal (the user, 2026-10-24): a changed quantity or product uses the existing "Your order has changed since you reviewed it. Please check it and place it again."; a withdrawn way to pay opens the payment step with the payment step's own "This way to pay is not offered. Please choose another one."
- answered: the "… item(s) unavailable or out of stock" sentence for changed prices (two-orders-never-sell-more-than-the-stock#2) is reached by no path — no check produces a price-change error — so no customer sees it; on the user's say-so (2026-10-24) it moved to `the-checkout-code-findings-are-closed` as unreachable code.
- answered: the shipping-method refusal was checked (2026-10-24) and needs no change — see Out of scope.

## Notes

- From the findings triage of 2026-10-23 (WP-151): a finding that changes behaviour is a story, not a task.
- The happy path is the changed order, not the withdrawn way to pay: it is what a customer can bring about in a browser; the withdrawn way to pay is tested integrated.
- Twin: the shop that already says it right adopts the criterion.
