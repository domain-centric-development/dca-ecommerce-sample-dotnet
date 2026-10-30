---
id: the-notice-reaches-an-encoded-return-page-accept-1
story: the-notice-reaches-an-encoded-return-page
stage: document
kind: acceptance
asked: 2026-10-30T09:03:53Z
digest: 2822bcdc2e3da807185ad74eb4dd36bd33c2aa5d96795f9dd8aa14f5d6912e47
---

# Accept the-notice-reaches-an-encoded-return-page?

## Question
Every gate passed. Look at what the story delivers before it counts as delivered:

- the-notice-reaches-a-return-page-with-an-encoded-address: Given a visitor who opened `/register?returnUrl=/products?q=clean%20code`; When they register; Then the catalogue page opens and shows that the account was created — `DcaShop.E2eTests.RegistrationE2eTest#TheCatalogueOpenedUnderAnEncodedAddressSaysTheAccountWasCreated`
- the-notice-reaches-a-return-page-with-a-fragment: Given a visitor who opened `/register?returnUrl=/products%23top`; When they register; Then the catalogue page opens and shows that the account was created — `DcaShop.IntegrationTests.RegistrationPageTest#ARegistrationOpenedForTheCatalogueWithAFragmentSaysThereThatTheAccountWasCreated`

Start the application with `dotnet run --project src/DcaShop.Web`.

## Options
- accepted: the story is delivered.
- a correction: what should be different, written into the story (criteria and an `answered:` line naming this record); the story runs again from plan.
