# DCA Shop — technical decisions

## Stack

C# on .NET 10 (the SDK pinned in `global.json`) with ASP.NET Core MVC, one project per bounded
context, xUnit for the tests. The DCA building blocks and the DCA architecture rules come as
published packages. It is the .NET twin of the Java reference implementation and follows its
behaviour.

## Frontend approach

Server-rendered Razor views, translated one to one from the Java sample's templates, with the same
stylesheet and no client framework. The REST API and the MCP server are separate surfaces over the
same use cases.

## Persistence

In-memory only, behind the same ports as in the Java sample. No database.

## Runtime

One ASP.NET Core process on port 5080, started with `dotnet run --project src/DcaShop.Web` or as a
container (`docker compose up`).

It is built to run as several instances behind a load balancer without sticky sessions: nothing a later request
needs lives in one instance's memory. A message for the page a redirect leads to travels with the redirect as a
fixed code in its URL (`?notice=<code>`), never as the sentence and never in a server session: the page turns the code
into its own sentence and shows nothing for a code it does not know. The valid codes are one enum in the incoming
web adapter, each value with its code and its sentence; reading the parameter is a lookup in that enum, never a
switch over strings. The checkout steps' notices are `CheckoutStepNotice`. What must outlast a request lives in the
database or in a cookie every instance can read.

## Two shops, one shop

This shop has a twin in the other language that implements the same specification. Where a plan shapes an
element differently from the twin (another mechanism, another split, another name), the plan's `## Notes`
answers three questions, "none" included: does the knowledge catalog need a node, could a rule check it, would
a marker make it checkable. The difference is closed at that source before it is closed in the code.

## Integrations

A payment provider over REST, outbound. A payment is `POST /payments` with the body
`{"amount": "17.98", "currency": "EUR"}` — the amount a decimal string with two places, never a JSON number,
which would pass through binary floating point — and the currency an ISO 4217 code;
`201` with a payment reference authorizes it, `402` refuses it, and no answer within 2 seconds counts
as unavailable. Where the provider's address is not configured, a stand-in inside the sample takes
payments. MCP clients reach the catalogue over HTTP. Nothing else.

## Version policy

The current .NET LTS release and the latest stable ASP.NET Core packages. The DCA packages are
pinned to a released version.
