---
id: login-leads-only-to-shop-pages-accept-1
story: login-leads-only-to-shop-pages
stage: document
kind: acceptance
asked: 2026-10-21T15:30:13Z
digest: 2e0649604d7f2c6660eef93f4dc1101499113cf339483315abee07cda8cab29e
---

# Accept login-leads-only-to-shop-pages?

## Question
Every gate passed. Look at what the story delivers before it counts as delivered:

- the-register-link-drops-an-outside-return-page: Given a visitor with no cookies; When they open `/login?returnUrl=https://elsewhere.example/`; Then the "Register here" link leads to `/register` — `DcaShop.E2eTests.LoginE2eTest#TheRegisterLinkOfALoginOpenedForAnotherSiteLeadsToTheRegistrationAlone`

Start the application with `dotnet run --project src/DcaShop.Web`.

## Options
- accepted: the story is delivered.
- a correction: what should be different, written into the story (criteria and an `answered:` line naming this record); the story runs again from plan.

## Answer
answer: accepted
by: Christoph Bloemer
at: 2026-10-21T15:40:15Z
