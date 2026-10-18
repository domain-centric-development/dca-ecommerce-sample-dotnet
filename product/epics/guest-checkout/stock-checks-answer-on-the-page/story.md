---
id: stock-checks-answer-on-the-page
epic: guest-checkout
status: delivered
delivered: 2026-10-13T18:07:00Z
delivered_by: gate
created: 2026-10-13T19:17:38Z
context: checkout
feature: ordering
title: A stock check that fails answers on the page it came from
depends_on: [place-the-order-from-the-review-and-see, stock-goes-down-by-what-was-bought]
---

# A stock check that fails answers on the page it came from

## Story

As a guest whose products ran short while I was shopping I want the shop to tell me so on the page I acted on,
so that I can change my order instead of landing on an error page.

## Acceptance criteria

### Rule: A cart with a line short of stock cannot start the checkout

#### start-from-an-outdated-cart-page-is-refused
- Given a guest whose cart page, opened while it held "Wooden Hexagon Desk Model" five times with 8 in stock,
  still shows "Proceed to Checkout"
- And another visitor has since bought it five times, leaving 3
- When the guest presses "Proceed to Checkout" on that page
- Then the cart page opens with the error "Cannot start checkout, 1 item(s) unavailable or out of stock"

### Rule: Place Order checks every line again and changes nothing when one fails

#### place-order-is-refused-when-stock-ran-short (happy path)
- Given a guest at the review with "Wooden Hexagon Desk Model" five times
- And another visitor has since bought it five times, leaving 3
- When the guest presses "Place Order"
- Then the review step stays open at `/checkout/review` and shows an error
- And no confirmation is shown
- And the product page still reads "3 units available" and the guest's cart still holds the product five
  times

#### place-order-is-refused-when-sold-out
- Given a guest at the review with "Wooden Hexagon Desk Model" twice
- And another visitor has since bought all 8
- When the guest presses "Place Order"
- Then the review step stays open at `/checkout/review` and shows an error
- And no confirmation is shown

## Changed expectations

- Proceeding to checkout from an outdated cart page whose line ran short of stock ends on the error page now, and
  opens the cart page with "Cannot start checkout, 1 item(s) unavailable or out of stock" after.
- A refused Place Order leaves the browser at `/checkout/confirm` now and at `/checkout/review` after, with its error
  and nothing changed, before and after.

## Notes

- The shop in the other language already behaves like this; this story closes the gap so that both hold the same spec
  (WP-107, decision `starting-and-placing-check-the-cart-01`).
