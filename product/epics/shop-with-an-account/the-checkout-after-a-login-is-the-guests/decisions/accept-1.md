---
id: the-checkout-after-a-login-is-the-guests-accept-1
story: the-checkout-after-a-login-is-the-guests
stage: document
kind: acceptance
asked: 2026-11-11T04:02:58Z
digest: 2dd1aee44792dbf3447bc53e8d83d4727186c8654b9a154b3932a31ba6e92632
---

# Accept the-checkout-after-a-login-is-the-guests?

## Question
Every gate passed. Look at what the story delivers before it counts as delivered:

- a-login-returns-to-the-guests-checkout-not-an-older-one: Given an account with an open checkout from a cart it emptied since; And a guest who started a checkout of "Clean Architecture"; When the guest logs in to that account from the checkout; Then the checkout shows "Clean Architecture", not the account's older checkout — `DcaShop.E2eTests.LoginFromTheCheckoutE2eTest#LoggingInFromTheCheckoutShowsTheGuestsCheckoutNotTheAccountsOlderOne`

Start the application with `dotnet run --project src/DcaShop.Web`.

## Options
- accepted: the story is delivered.
- a correction: what should be different, written into the story (criteria and an `answered:` line naming this record); the story runs again from plan.
