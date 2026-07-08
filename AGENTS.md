# TBankAcquiringNet Working Agreement

This file gives AI coding agents and maintainers the project-specific rules that matter most.

## Purpose

`TBankAcquiringNet` is a .NET SDK for T-Bank acquiring integrations.

The SDK should provide typed clients, request/response models, signing helpers, notification validation, and transport configuration for T-Bank acquiring APIs. It should not contain application-specific business rules, database persistence, ASP.NET Core endpoint code, ORMs, or workflows.

## Project Layout

The repository contains a single package:

```text
src/TBankAcquiringNet
```

Multisplit provider shop registration and payout packages now live in separate repositories, not here.

Future adapter packages should be added only when framework-specific behavior is actually needed:

```text
src/TBankAcquiringNet.AspNetCore
src/TBankAcquiringNet.Testing
```

Target `netstandard2.0`, `net8.0`, and `net10.0`. Changing the set of target frameworks is an explicit product decision.

## Package Boundary Rules

- Minimise dependencies in each SDK package.
- Use `HttpClient` and BCL APIs as the default transport foundation.
- Keep ASP.NET Core, EF Core, Dapper, ORMs, queues, hosting, and persistence behavior out of the SDK package.
- Do not add a shared `Abstractions` or root runtime package until duplication or public API pressure proves it is needed.
- Keep public APIs centered on typed clients, options, request models, response models, status enums, error details, signing helpers, notification models, and validation results.
- Do not expose raw stringly-typed dictionaries for known T-Bank fields unless the API explicitly supports extension data.
- Preserve T-Bank wire names and casing in serialization models.
- Keep application-specific concepts out of package names, namespaces, and public API.

## Payments Package Rules

`TBankAcquiringNet` covers payment acquiring and multisplit payment acceptance.

Initial scope should focus on:

```text
Init
GetState
Confirm
Cancel
GetQr
ChargeQr
payment status notifications
account binding notifications
token generation and validation
```

Multisplit provider shop registration and payout flows live in separate repositories and are out of scope for this package.

## Security And Signing Rules

- Never log terminal passwords, bearer tokens, card data, CVV, private keys, certificates, or generated request tokens.
- Treat `Password`, OAuth credentials, bearer tokens, certificate private keys, and card data as secrets.
- Keep token/signature generation deterministic and covered by tests with known examples.
- Prefer explicit redaction helpers for diagnostics and exceptions.
- Do not persist secrets or add sample real credentials to the repository.
- Avoid APIs that make it easy to accidentally send raw PAN/CVV unless the T-Bank flow explicitly requires it.

## Testing Expectations

SDK changes need focused tests for:

- JSON field names and casing
- optional and required fields
- token generation ordering and formatting
- notification token validation
- status parsing
- endpoint URL selection for test and production environments
- HTTP request method, path, body, and headers
- T-Bank error responses
- cancellation token flow
- invalid configuration validation

Always run:

```bash
dotnet test TBankAcquiringNet.slnx
```

For package-facing changes, also run:

```bash
dotnet pack TBankAcquiringNet.slnx --configuration Release --output artifacts/packages
```

## Documentation Expectations

Keep `README.md` short and package-user focused.

Use Russian XML comments for public T-Bank-specific enums, status values, error-code helpers, and exception types when the comment describes bank-domain behavior. Keep implementation comments sparse and only where they clarify non-obvious code.

Put detailed material in docs:

- payments design notes: `docs/payments-design.md`
- roadmap and implementation status: `docs/roadmap.md`
- error-handling policy: `docs/error-handling.md`
- test and credential handling notes: `docs/testing.md`
- release process: `docs/release.md`

Update `CHANGELOG.md` for notable public behavior, API, packaging, or documentation changes once the first public package version exists.

## Release Discipline

Use Semantic Versioning.

Preview versions:

```text
0.1.0-preview.1
0.2.0-preview.1
```

Release tags use the package version prefixed with `v`:

```text
v0.1.0-preview.1
```

Do not call the packages `1.0.0` until at least one real application integration validates the public API shape.
