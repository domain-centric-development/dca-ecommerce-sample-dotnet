---
id: the-profile-refusal-names-today-accept-1
story: the-profile-refusal-names-today
stage: document
kind: acceptance
asked: 2026-11-11T02:41:40Z
digest: dcf885ef517d80d9ecae63e564cf7d68959510d0726033368e1cee8dacfee895
---

# Accept the-profile-refusal-names-today?

## Question
Every gate passed. Look at what the story delivers before it counts as delivered:

- todays-date-of-birth-is-refused-in-words-that-name-it: Given a customer logged in on the profile page; When they save today's date as the date of birth; Then the profile shows "Please enter a date of birth in the past." — `DcaShop.E2eTests.ProfileE2eTest#TodayAsDateOfBirthIsRefusedAsNotInThePast`
- future-date-is-refused-in-the-profile: Given the customer "anna.schmidt@example.com" is logged in on `/account/profile`; When they save Date of Birth 2999-01-01; Then the profile shows "Please enter a date of birth in the past." — `DcaShop.IntegrationTests.ProfileDateOfBirthRefusalPageTest#AFutureDateOfBirthIsRefusedAsNotInThePast`

Start the application with `dotnet run --project src/DcaShop.Web`.

## Options
- accepted: the story is delivered.
- a correction: what should be different, written into the story (criteria and an `answered:` line naming this record); the story runs again from plan.
