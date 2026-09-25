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

## Integrations

A payment provider over REST, outbound. A payment is `POST /payments` with the amount and currency;
`201` with a payment reference authorizes it, `402` refuses it, and no answer within 2 seconds counts
as unavailable. Where the provider's address is not configured, a stand-in inside the sample takes
payments. MCP clients reach the catalogue over HTTP. Nothing else.

## Version policy

The current .NET LTS release and the latest stable ASP.NET Core packages. The DCA packages are
pinned to a released version.
