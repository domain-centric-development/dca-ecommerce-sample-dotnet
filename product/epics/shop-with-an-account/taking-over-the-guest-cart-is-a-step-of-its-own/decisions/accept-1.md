---
id: taking-over-the-guest-cart-is-a-step-of-its-own-accept-1
story: taking-over-the-guest-cart-is-a-step-of-its-own
stage: document
kind: acceptance
asked: 2026-10-14T21:29:40Z
digest: 27b91cf43c9c5b9dab0aab9dd8f8ff762f717c6753a48d42f7ba8fdd2e0cb112
---

# Accept taking-over-the-guest-cart-is-a-step-of-its-own?

## Question
Every gate passed. Look at what the story delivers before it counts as delivered:

- the-page-after-the-login-moves-no-cart: Given the account "anna.schmidt@example.com" with password "Secret123", registered earlier, whose cart holds nothing; And a browser with no cookies that runs no script, in which a guest added `"Ubiquitous Language" Mug` to the cart; When the guest logs in on `/login` with "anna.schmidt@example.com" and "Secret123"; Then a page opens that says "Bringing your cart along." and offers a "Continue" button; And the account's cart still holds nothing — `DcaShop.IntegrationTests.CartTakeOverPageTest#OpeningThePageAfterTheLoginOffersTheTakeOverAndMovesNoCart, DcaShop.IntegrationTests.CartRecoveryOnLoginTest#OpeningThePageAfterTheLoginLeavesTheAccountsCartEmpty`
- continue-takes-the-guest-cart-over: Given the account "anna.schmidt@example.com", whose cart holds nothing, and a guest in a browser that runs no script; When they press "Continue"; Then the cart page `/cart` opens and shows "Welcome back!"; And the cart shows one line `"Ubiquitous Language" Mug` with quantity 1 — `DcaShop.E2eTests.GuestCartTakeOverE2eTest#PressingContinueAfterTheLoginBringsTheGuestsCartIntoTheAccount`

Start the application with `dotnet run --project src/DcaShop.Web`.

## Options
- accepted: the story is delivered.
- a correction: what should be different, written into the story (criteria and an `answered:` line naming this record); the story runs again from plan.

## Answer
answer: accepted
by: Christoph Bloemer
at: 2026-10-14T21:31:28Z
