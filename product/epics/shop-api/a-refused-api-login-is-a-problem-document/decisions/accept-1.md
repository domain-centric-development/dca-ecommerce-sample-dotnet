---
id: a-refused-api-login-is-a-problem-document-accept-1
story: a-refused-api-login-is-a-problem-document
stage: document
kind: acceptance
asked: 2026-10-15T10:52:04Z
digest: 19d891af429ba76e6dfde4be794971f7a8f966a9bff5da8edb161e8cb2b6ee8b
---

# Accept a-refused-api-login-is-a-problem-document?

## Question
Every gate passed. Look at what the story delivers before it counts as delivered:

- a-wrong-password-is-refused: Given an account "ada@example.com" with password "Secret123"; When a client sends `POST /api/auth/login` with `{"email":"ada@example.com","password":"Wrong1234"}`; Then the shop answers 401 with an `application/problem+json` body and the header `WWW-Authenticate: Bearer`; And its title is "Authentication failed" and it carries no detail — `DcaShop.E2eTests.LoginApiE2eTest#AWrongPasswordIsAProblemDocumentWithTheBearerChallenge`

Start the application with `dotnet run --project src/DcaShop.Web`.

## Options
- accepted: the story is delivered.
- a correction: what should be different, written into the story (criteria and an `answered:` line naming this record); the story runs again from plan.

## Answer
answer: accepted
by: Christoph Bloemer
at: 2026-10-15T14:56:00Z
rationale: Accepted by the person (2026-10-15), recorded by the managing session.
