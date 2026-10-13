---
id: forms-refuse-a-post-without-token-with-403-accept-1
story: forms-refuse-a-post-without-token-with-403
stage: document
kind: acceptance
asked: 2026-10-13T06:54:57Z
digest: af941b09c04c791c48efad2f7a2cd9ad36c4ad599da1975246ac2cd71cb547ae
---

# Accept forms-refuse-a-post-without-token-with-403?

## Question
Every gate passed. Look at what the story delivers before it counts as delivered:

- a-login-post-without-the-form-token-is-refused: Given an account "ada@example.com" with password "Secret123"; When a browser posts email "ada@example.com" and password "Secret123" to `/login` without the form token the login page carries; Then the shop answers 403; And the visitor is not logged in: the header still shows "Login" and "Register" — `DcaShop.E2eTests.FormTokenRefusalE2eTest#ALoginPostedWithoutTheTokenOfTheLoginPageIsForbiddenAndLogsNobodyIn`
- a-registration-post-without-the-form-token-is-refused: Given no account exists for "grace@example.com"; When a browser posts a complete, valid registration for "grace@example.com" to `/register` without the form token; Then the shop answers 403; And logging in as "grace@example.com" afterwards fails with "Invalid email or password" — `DcaShop.IntegrationTests.FormTokenRefusalTest#ARegistrationPostedWithoutTheFormTokenIsForbiddenAndCreatesNoAccount`
- a-logout-post-without-the-form-token-is-refused: Given a customer logged in as "ada@example.com"; When a post to `/logout` arrives from her browser without the form token; Then the shop answers 403; And the header still shows "Welcome, ada@example.com" — `DcaShop.IntegrationTests.FormTokenRefusalTest#ALogoutPostedWithoutTheFormTokenIsForbiddenAndKeepsTheCustomerLoggedIn`

Start the application with `dotnet run --project src/DcaShop.Web`.

## Options
- accepted: the story is delivered.
- a correction: what should be different, written into the story (criteria and an `answered:` line naming this record); the story runs again from plan.

## Answer
answer: accepted
by: Christoph Bloemer
at: 2026-10-13T07:30:03Z
