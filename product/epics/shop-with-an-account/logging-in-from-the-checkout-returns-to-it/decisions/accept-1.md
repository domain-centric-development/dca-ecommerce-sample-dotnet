---
id: logging-in-from-the-checkout-returns-to-it-accept-1
story: logging-in-from-the-checkout-returns-to-it
stage: document
kind: acceptance
asked: 2026-10-20T19:06:43Z
digest: 24969e6329161861c94f759c23ab574760dd5fc9f408c94e841fd68b5eb85333
---

# Accept logging-in-from-the-checkout-returns-to-it?

## Question
Every gate passed. Look at what the story delivers before it counts as delivered:

- login-from-the-checkout-keeps-the-cart-not-the-checkout: Given the account "anna.schmidt@example.com", registered earlier, whose cart holds nothing; And a browser with no cookies in which a guest added "Domain-Driven Design" and pressed "Proceed to Checkout"; And the guest opened `/login?returnUrl=/checkout/buyer`; When they log in with "anna.schmidt@example.com" and "Secret123"; Then the buyer step `/checkout/buyer` opens; And its order summary shows "Domain-Driven Design x1" — `DcaShop.E2eTests.LoginFromTheCheckoutE2eTest#LoggingInFromTheBuyerStepReturnsToItWithTheGuestsOrder`

Start the application with `dotnet run --project src/DcaShop.Web`.

## Options
- accepted: the story is delivered.
- a correction: what should be different, written into the story (criteria and an `answered:` line naming this record); the story runs again from plan.

## Answer
answer: accepted
by: Christoph Bloemer
at: 2026-10-21T06:29:23Z
