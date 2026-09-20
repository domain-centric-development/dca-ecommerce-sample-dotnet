# ADR-015: A Committed Default Must Not Start a Real Deployment

**Date**: 2026-09-20 · **Status**: Accepted

## Context

`appsettings.json` is committed, and three of the values in it are ones no deployment may keep: `Jwt:Secret`,
the operator credentials `admin`/`admin` under `Backoffice`, and `SecureCookies: false` on both the shop's cookies
and the operator session. They make the sample start without configuration, which is the point; they also make it
start that way anywhere else, which nothing prevented.

A committed secret is a published secret: whoever can read this repository can mint a token the deployment
accepts. The operator login replays failed event publications, so it is the most privileged door in the shop.
Cookies without `Secure` travel over plain HTTP, where the token is readable in transit.

None of the three announces itself — the application starts, the pages render, the behaviour is identical to a
development run. And the image defaults to `Production`, so the deployment most likely to keep these values is the
one least entitled to.

## Decision

**The development values are named constants, and the shop refuses to start on them outside `Development`.**

- `JwtOptions.DevelopmentSecret`, `BackofficeOptions.DevelopmentUsername` and `DevelopmentPassword` hold what
  `appsettings.json` carries. `DevelopmentDefaultsTest` pins the JSON to the constants: a guard comparing against
  a value nobody uses refuses nothing.
- `JwtDevelopmentDefaultsValidator : IValidateOptions<JwtOptions>` and
  `BackofficeDevelopmentDefaultsValidator : IValidateOptions<BackofficeOptions>` refuse the shipped values of
  their own context and name the variable that supplies a real one — `Jwt__Secret`, `Jwt__SecureCookies`,
  `Backoffice__Username`/`Backoffice__Password`, `Backoffice__SecureCookies`. A fail-fast an operator cannot act
  on is only an outage.
- **`ValidateOnStart`, not on first use.** A configuration refused while the deployment is watched is a rollback;
  the same refusal on a visitor's first request is an incident. Each context registers its own validator where it
  binds its own options.
- The input is `IHostEnvironment.IsDevelopment()`, whose default is `Production`: an application that is not told
  what it is treats itself as real. `compose.yaml` already says `ASPNETCORE_ENVIRONMENT: Development`, as does
  `launchSettings.json` and the test host, so the demo and the suites are unaffected; `docker run` of the image is
  a production run and now refuses to start.

The Java twin decides the same rule from the active Spring profile (`dev | inmemory`, its ADR-043), because that
is the platform's own switch for the same question.

## Consequences

- Positive: `UnsafeDefaultsTest` starts the real host under `Production` and asserts each refusal. It is red with
  the two `AddSingleton<IValidateOptions<…>>` lines removed — the failure mode that matters, because a validator
  nobody registers refuses nothing and the class itself looks identical either way.
- Positive: `Configure<T>` became `AddOptions<T>().Bind(…).ValidateOnStart()`, so every option group now fails at
  startup rather than at first injection — including `JwtOptions.Validate()`, which until now could have surfaced
  on a request.
- Negative: the guard compares against known strings. `Jwt__Secret=secret` passes it. This catches the value that
  ships, not weak configuration in general.
- Neutral: the operator cookie's `Secure` flag is an adapter option here and a container property in the Java
  sample (`server.servlet.session.cookie.secure`). Two mechanisms, one rule; the validators word the refusal
  alike.

## Harness questions

**Catalog**: done — a new pitfall, *A committed default that only a comment forbids*, written from both samples:
the defect, the startup refusal, and the two traps this work hit (a guard compared against a literal nobody uses;
a validator that is written but never registered). **Rule**: none that is mechanical — recognising "this constant
is also a fallback in configuration" is a string match across two file formats, not a structural property.
**Marker**: none. Configuration hygiene is not a building block.
