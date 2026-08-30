# ADR-001: One Project per Bounded Context, Layers as Folders

**Date**: 2026-08-30 · **Status**: Accepted

## Context

.NET offers two ways to make architectural boundaries physical: projects (compiler-enforced references) and
namespaces (convention, enforced by tests). DCA distinguishes bounded contexts *and* layers inside a context.
Enforcing both with projects would mean four to six projects per context.

## Decision

- **One project (assembly) per bounded context** plus `SharedKernel`, `Infrastructure` and the `Web` host.
  Context isolation is therefore compiler-enforced: a context can only see what it references, and it references
  another context solely to reach its `Api/` and `Events/` namespaces.
- **Layers are folders/namespaces inside the context project** (`Domain`, `Application`, `Adapter`, `Api`,
  `Events`, `Infrastructure`). Their rules are enforced by `DomainCentric.ArchRules` in the architecture tests.
- Incoming web adapters (MVC controllers, view models) live in the context project so the rules see them; the
  Razor views live in the host, addressed by explicit paths (`~/Views/Cart/Cart.cshtml`).

## Consequences

- Positive: few projects, fast builds, contexts still cannot bypass each other; the namespace layout mirrors the
  Java package layout one to one, so the guide's templates apply to both languages.
- Negative: layer violations inside a context compile — they fail only in the architecture tests. That is the
  same trade-off the Java sample makes.
- The in-memory repositories (`ConcurrentDictionary`) are demonstration stubs, not a persistence pattern: the
  dictionary is thread-safe, the aggregate instance it hands out is shared and mutable, so two concurrent requests
  on the same cart or checkout session can interleave; read-then-create flows such as `GetOrCreateActiveCart` are
  not atomic. A real adapter would map to persistent state with optimistic concurrency (a version on the
  aggregate, checked in `SaveAsync`) — nothing in the domain or application layer has to change for that.
