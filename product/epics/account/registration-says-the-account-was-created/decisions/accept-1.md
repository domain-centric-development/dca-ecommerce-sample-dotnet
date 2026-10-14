---
id: registration-says-the-account-was-created-accept-1
story: registration-says-the-account-was-created
stage: document
kind: acceptance
asked: 2026-10-14T16:45:32Z
digest: 7da37870e22c431aa4cc62fed278e713963f20bd5c6a07b43369f19e005567cb
---

# Accept registration-says-the-account-was-created?

## Question
Every gate passed. Look at what the story delivers before it counts as delivered:

- registration-says-the-account-was-created: Given a visitor with no cookies on `/register`; When they submit First Name "Anna", Last Name "Schmidt", Date of Birth 1990-05-17, Email "anna.schmidt@example.com",; Then the home page `/` opens and shows "Account created successfully! Welcome!" — `DcaShop.E2eTests.RegistrationE2eTest#TheHomePageARegistrationLeadsToSaysTheAccountWasCreated`
- the-page-returned-to-says-the-account-was-created: Given a visitor with no cookies who opened `/register?returnUrl=/products`; When they submit a valid registration for "anna.schmidt@example.com"; Then the catalogue page `/products` opens and shows "Account created successfully! Welcome!" — `DcaShop.IntegrationTests.RegistrationPageTest#ARegistrationOpenedForTheCatalogueSaysThereOnceThatTheAccountWasCreated`

Start the application with `dotnet run --project src/DcaShop.Web`.

## Options
- accepted: the story is delivered.
- a correction: what should be different, written into the story (criteria and an `answered:` line naming this record); the story runs again from plan.

## Answer
answer: accepted
by: Christoph Bloemer
at: 2026-10-14T17:06:46Z
