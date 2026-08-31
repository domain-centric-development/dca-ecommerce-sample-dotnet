# Glossary — Bounded Context: Account

> **Bootstrap draft** — Automatically extracted from the current domain code.
> Terms reflect the as-is implementation, not necessarily the target Ubiquitous Language.
> Conflicts and polysemy are marked under "Notes" and should be resolved with the business
> stakeholder before this draft is adopted as authoritative.

Sources: `DcaShop.Account.Domain.{Model,Event,Gateway}`.

---

## Aggregate Roots

### Account

**Definition:** Registered user account with credentials and profile. Links a cross-context
identity (`UserId`) to a context-local aggregate identity (`AccountId`) and encapsulates
authentication, roles, and the lifecycle (active, suspended, closed).

**Type:** Aggregate Root

**Identity:** `AccountId`

**Related terms:** `Email`, `Owner`, `HashedPassword`, `AccountStatus`, `UserId`,
`PasswordHasher`, `Role` (planned).

**Operations:** `Register`, `Reconstitute`, `CheckPassword`, `RecordLogin`, `ChangePassword`,
`ChangeEmail`, `ChangeOwnerDateOfBirth`, `Suspend`, `Reactivate`, `Close`, `AddRole`,
`RemoveRole`.

**Notes:** The account belongs to an **`Owner`**, whose **name is fixed at registration**: no
operation on the aggregate accepts an `Owner` or mentions a name, so the only way a name enters the
system is `Register`. The date of birth of that same owner may be corrected later
(`ChangeOwnerDateOfBirth`). Distinct from the buyer's name in the `checkout` context (`BuyerInfo`),
which is per-order shipping data and freely editable — same words, different concept, different
lifecycle. `Roles` is currently an `IReadOnlySet<string>` over the constants of `Role`. Planned
cleanup: promote `Role` to a Value Object (instead of `string`) to enforce valid values and improve
domain vocabulary. On registration the
`UserId` from the JWT is preserved unchanged to guarantee cart/checkout continuity across the
guest → account transition.

---

## Value Objects

### Email

**Definition:** Validated, normalized (lower-cased) email address that also serves as the unique
login credential.

**Type:** Value Object

**Operations:** `of`, `localPart`, `domain`.

### Owner

**Definition:** The natural person an account belongs to: first name, last name and date of birth.
The **name identifies who the account belongs to and is therefore fixed for the lifetime of the
account** — it is captured once at registration and no operation replaces it. Only the date of birth
may be corrected, via `WithDateOfBirth`, which carries both names over unchanged.

**Type:** Value Object

**Operations:** `Of`, `WithDateOfBirth`, `FullName`.

**Notes:** Names are trimmed but never otherwise normalized — capitalization, particles and spelling
of a person's own name are not the account's to correct — and are limited to 100 characters. A date
of birth is mandatory and may not lie in the future. Immutability of the name is a property of the
type rather than a rule callers must remember: there is no setter, and the one derivation
(`WithDateOfBirth`) copies the names. The date rule itself lives in the **Usable Date of Birth**
specification below.

### HashedPassword

**Definition:** Securely hashed password; encapsulates the hash value, strength validation
against minimum requirements, and comparison operations. Never stores plaintext and delegates
the actual cryptographic operation to the `PasswordHasher` domain gateway.

**Type:** Value Object

**Related terms:** `IPasswordHasher` (Domain Gateway in `DcaShop.Account.Domain.Gateway`).

**Operations:** `FromPlaintext`, `FromHash`, `Matches`, `ValidatePasswordStrength`.

**Notes:** `ToString()` is deliberately masked. The rules themselves are named below as **Password
Policy**; change them there and here together.

### Password Policy

**Definition:** The rules a plaintext password must satisfy before it may become a `HashedPassword`:
at least `MinLength` (8) characters, at most `MaxByteLength` (72) bytes when UTF-8 encoded, and at
least one uppercase letter, one lowercase letter and one digit.

**Type:** Domain rule, encoded in `HashedPassword.ValidatePasswordStrength`. Not yet a Specification —
promote it to one if a second component needs to evaluate the policy without creating a password.

**Related terms:** `HashedPassword`, `PasswordHasher`.

**Notes:** The maximum is expressed in **bytes, not characters**, because that is the bound hashing
algorithms impose — BCrypt rejects input beyond 72 bytes. Keeping the policy at that value means an
over-long password is refused as a rule with a message meant for the user, and can never reach
`IPasswordHasher` and fail there as a technical fault. The `maxlength="72"` on the password inputs of
`Views/Account/Register.cshtml` and `Views/Account/ChangePassword.cshtml` is a convenience hint only: the browser counts
characters, so a multi-byte password within that hint can still be refused by the policy.

### Usable Date of Birth

**Definition:** A date of birth is usable when it is known and does not lie in the future.

**Type:** Specification (`DcaShop.Account.Domain.Specification.UsableDateOfBirth`, implements
`ISpecification<DateOnly?>`)

**Related terms:** `Owner`, `Account`.

**Operations:** `IsSatisfiedBy`, `RequireSatisfiedBy`.

**Notes:** A first-class rule because two components evaluate it: `Owner` refuses to exist without a
usable date, and `ChangeProfileUseCase` rejects a submitted one **before** touching the aggregate, so
a refused submission cannot leave a half-applied change behind. `RequireSatisfiedBy` carries the
user-facing message and names which half of the rule failed. Deliberately says nothing about
plausible ages — an arbitrary upper bound would refuse real people.

### AccountId

**Definition:** Aggregate-internal identifier of an account. Distinct from `UserId`; an account
has exactly one `AccountId` and is linked to exactly one `UserId`.

**Type:** Value Object (ID)

**Operations:** `Of`, `Generate`.

### AccountStatus

**Definition:** Lifecycle status of an account: `ACTIVE` (can log in), `SUSPENDED` (temporarily
blocked), `CLOSED` (permanently ended).

**Type:** Value Object (Enum)

**Operations:** `CanLogin`, `IsTerminal`.

### Role (planned)

**Definition:** Planned Value Object for a business role of the account (currently modelled as
`String` in `Account.roles`, e.g. `CUSTOMER`).

**Type:** Value Object (planned) — today a `static class Role` holding the two constants
`Role.Customer` and `Role.Staff`.

**Synonyms (avoid):** raw `string` role.

**Notes:** DCA review recommendation: migrate `IReadOnlySet<string> Roles` → a set of `Role` values to
encapsulate permitted values and rule out typos at compile time.

### Token (planned)

**Definition:** Planned Value Object for authentication/refresh tokens that today flow through
the adapters implicitly as strings.

**Type:** Value Object (planned)

**Notes:** Not currently present in the domain model. If tokens have business meaning in the
Account context (e.g. refresh-token rotation, password-reset token), model them as a VO.

### UserId

**Definition:** Cross-context user identity (Shared Kernel) that uniformly identifies anonymous
and registered users and is carried in the JWT.

**Type:** Value Object (ID, Shared Kernel)

**Related terms:** `Account`, `AccountId`, `AccountRegistered`, `AccountLinkedToIdentity`.

**Notes:** Lives in the `sharedkernel` and is consumed here. Resolve the polysemy with
`CustomerId` in the `checkout`/`cart` contexts — often semantically identical, formally
separate.

---

## Domain Events

### AccountRegistered

**Definition:** A new account has been registered; contains `AccountId`, `Email`, the `Owner` and
the linked `UserId`. The owner travels with the event so a consumer (welcome mail, analytics) can
address the person by name without querying back.

**Type:** Domain Event

**Related terms:** `Account`, `Email`, `Owner`, `UserId`.

### AccountLinkedToIdentity

**Definition:** The cross-context `UserId` has been linked to an `AccountId` — signals to other
contexts that the `UserId` now belongs to a registered user (e.g. for cart takeover).

**Type:** Domain Event

**Related terms:** `Account`, `UserId`.

### AccountLoggedIn

**Definition:** A user has successfully logged in to their account.

**Type:** Domain Event

### AccountPasswordChanged

**Definition:** The password of an account has been changed.

**Type:** Domain Event

### AccountEmailChanged

**Definition:** The email address of an account has been changed; carries the previous and the new
address. Since the email is also the login credential, the identity token is re-issued afterwards.

**Type:** Domain Event

**Related terms:** `Account`, `Email`.

### AccountOwnerDateOfBirthChanged

**Definition:** The date of birth of an account's owner has been corrected; carries the previous and
the new date, because a correction is only interpretable against the value it replaced. There is
deliberately no counterpart for the owner's name, because no operation changes it.

**Type:** Domain Event

**Related terms:** `Account`, `Owner`.

### AccountSuspended

**Definition:** An account has been temporarily blocked and can no longer log in.

**Type:** Domain Event

### AccountReactivated

**Definition:** A previously suspended account has been re-enabled.

**Type:** Domain Event

### AccountClosed

**Definition:** An account has been permanently closed; terminal state of the lifecycle.

**Type:** Domain Event

---

## Domain Gateways

### PasswordHasher

**Definition:** Domain gateway for hashing plaintext passwords and timing-safe comparison
against a stored hash. The interface belongs to the domain (`DcaShop.Account.Domain.Gateway`) and is
invoked by the `Account` aggregate as well as `HashedPassword` when a rule requires hashed
credentials. The technical implementation (BCrypt/Argon2/...) lives in the adapter.

**Type:** Domain Gateway (implements `IDomainGateway`)

**Location:** `DcaShop.Account.Domain.Gateway.IPasswordHasher`

**Implementation:** `BcryptPasswordHasher` in `DcaShop.Account.Adapter.Outgoing.Security`
(BCrypt.Net).

**Related terms:** `HashedPassword`, `Account`.

**Operations:** `Hash(string) → string`, `Matches(string, string) → bool`.

**Notes:** Follows Vernon (IDDD `iddd_identityaccess` sample, where the `User` aggregate calls
`EncryptionService` via `DomainRegistry`). The service locator is replaced by a typed parameter
that the use case injects into the aggregate. Classification of the *service* remains
Infrastructure (Vernon, IDDD Ch. 7); classification of the *interface* is Domain Gateway,
because it is consumed by domain code and expressed in domain language.

---

## Open issues from DCA review

- **`Role` as a VO instead of `string`** — replace `Account.Roles : IReadOnlySet<string>` with a set
  of `Role` values.
- **Introduce `Token` as a VO** if tokens have business relevance (e.g. refresh,
  password reset).
- **Polysemy `CustomerId` (Cart, Checkout) vs `UserId` (SharedKernel) vs `AccountId`** —
  decide authoritatively which identity is referenced in which context.
- **`Account.reconstitute` bypasses the immutable-name rule** — it is public and accepts an
  arbitrary `Owner`, so application code could rebuild an account under its existing `AccountId`
  with a different name and save it. No production code calls it today (the in-memory repository
  stores the aggregate itself), so this is theoretical until the first mapping persistence adapter
  lands. **To discuss:** static rule restricting the callers, narrower visibility, or accept it as a
  documented convention. Recorded as the open question in `ADR-028: The Account Owner's Name Is
  Immutable by Type, Not by Rule`.
- **Password policy** should be extracted as a standalone domain specification rather than
  being buried in `HashedPassword.validatePasswordStrength` — the same move already made for the
  date-of-birth rule (`UsableDateOfBirth`).
