# Changelog

All notable changes to `TBankAcquiringNet` will be documented in this file.

The project uses Semantic Versioning. Versions below `1.0.0` are preview releases and may include public API changes while the SDK contracts are validated against real integrations.

## Unreleased

No changes yet.

## 1.2.0

### Added

- Added `TBankInitDataKeys`, `TBankDeviceType`, and `TBankDeviceOs` constants for the T‑Pay/SberPay device parameters passed via the Init `DATA` object, and documented the wallet-specific `Init`/`Confirm`/`Cancel` behaviors (device params, one-time confirm, async `CONFIRMING`/`REVERSING`/`REFUNDING`) in XML remarks.

### Fixed

- `GetTinkoffPayQrAsync` and `GetSberPayQrAsync` now detect a JSON error envelope returned with HTTP 200 (for example an expired payment) and throw `TBankAcquiringProtocolException` carrying the T‑Bank `ErrorCode`/`Message`, instead of returning the JSON body as if it were an SVG image.

## 1.1.1

### Fixed

- `GetTinkoffPayQrAsync` and `GetSberPayQrAsync` now send `Accept: image/svg+xml` instead of the non-standard `image/svg`, which T-Bank rejected with HTTP 415 (surfaced as `TBankAcquiringProtocolException`). QR retrieval now succeeds.

## 1.1.0

### Added

- Added T‑Pay and SberPay methods to `TBankPaymentsClient`: `GetTinkoffPayStatusAsync`, `GetTinkoffPayLinkAsync`, `GetTinkoffPayQrAsync`, `GetSberPayQrAsync`, and `GetSberPayLinkAsync`. These are unsigned GET requests; the `link`/`status` methods return typed `Params`-wrapped responses and the `QR` methods return the SVG image as a string.
- Added Mir Pay and Alfa Pay methods to `TBankPaymentsClient`: `GetMirPayDeepLinkAsync` (returns the JWT-signed DeepLink) and `GetAlfaPayLinkAsync` (returns a `Params`-wrapped redirect link). Both are signed POST requests.
- Added `SendClosingReceiptAsync` (fiscalization) with separate typed receipt models per FFD version — `TBankReceiptFfd12` (FFD 1.2) and `TBankReceiptFfd105` (FFD 1.05) — passed via `TBankSendClosingReceiptFfd12Request` / `TBankSendClosingReceiptFfd105Request`. The request targets the `/cashbox/SendClosingReceipt` endpoint (host root, not the `/v2/` base). Added `PROCESSING`, `CHECKING`, `CHECKED`, `COMPLETING`, and `COMPLETED` to `TBankPaymentStatus`.

## 1.0.1

### Added

- Send a versioned `User-Agent` header on every request (e.g. `TBankAcquiringNet/1.0.0 (.NET 8.0)`). The version is read from the assembly and the header is set per request, so a caller-provided `HttpClient` is not mutated.

### Changed

- Lowered the `netstandard2.0` dependency floor from `9.0.0` to `System.Text.Json` `8.0.6` and `System.Net.Http.Json` `8.0.1` (current LTS, security-patched), so integrators are not forced onto the `9.0.0` graph. `net8.0`/`net10.0` are unaffected (in-box APIs, no package dependency).
- Documented concrete runtime support (.NET Framework 4.6.2+, .NET Core 2.0+, Mono, Unity) and a `.NET Framework` binding-redirects note in the READMEs.

### Fixed

- Regenerated the NuGet package icon to remove transparent whitespace on the right and bottom edges.

## 1.0.0

First stable release. Card payment and SBP payment flows are validated against real integrations, and the public API is now stable under Semantic Versioning. This release contains no API changes over `0.4.0-preview.1`.

## 0.4.0-preview.1

### Added

- Added SBP payment methods to `TBankPaymentsClient`: `GetQrState`, `GetQrBankList`, `QrMembersList`, `AddAccountQr`, `GetAddAccountQrState`, `GetAccountQrList`, and `SbpPayTest`, with typed request/response models and local request validation.
- Added `CANCEL_CHECKING` to `TBankPaymentStatus`.

### Changed

- Typed previously string-valued fields as enums: `TBankInitPaymentRequest.PayType` (`TBankPayType`), `.Language` (`TBankLanguage`), and `.Recurrent` (`TBankRecurrent`); and the account-binding `Status` fields (`TBankAccountQrStatus`).
- `TBankPaymentStatus` and `TBankAccountQrStatus` members now use the API's uppercase wire names (e.g. `CONFIRMED`, `PARTIAL_REFUNDED`; the `3DS_*` values map to `THREE_DS_CHECKING`/`THREE_DS_CHECKED`). **Breaking** for code referencing the old PascalCase members.
- An unrecognized payment or account-binding status now throws `NotImplementedException` pointing to the issue tracker, instead of silently mapping to `Unknown`. Known T-Bank misspellings (`PROCCESING`, `INACITVE`) are still tolerated on read.

## 0.3.0-preview.1

### Added

- Added a NuGet package icon, shipped alongside each package.
- Multi-targeted `TBankAcquiringNet` to `netstandard2.0`, `net8.0`, and `net10.0`. The `netstandard2.0` asset depends on the `System.Text.Json` and `System.Net.Http.Json` packages; `net8.0`/`net10.0` use the in-box APIs.

### Changed

- Renamed the package, namespace, project, and test projects from `TBankAcquiringNet.Payments` to `TBankAcquiringNet`. **Breaking:** update `using TBankAcquiringNet.Payments;` to `using TBankAcquiringNet;` and bump the package reference to `TBankAcquiringNet`.
- Rewrote the repository and package READMEs (in Russian) with an icon header, badges, configuration, two-stage payment, notification, and dependency-injection guidance.

### Removed

- Removed `TBankAcquiringNet.Multisplit.Shops` and `TBankAcquiringNet.Multisplit.Payouts` from this repository — they now live in separate repositories.

## 0.2.0-preview.3

### Changed

- Made `Kpp` optional in `TBankRegisterShopRequest` — KPP is not required by the T-Bank API.

## 0.2.0-preview.2

### Changed

- Added package-specific NuGet README files for `TBankAcquiringNet` and `TBankAcquiringNet.Multisplit.Shops` instead of packing the repository README into every package.

## 0.2.0-preview.1

### Added

- Started `TBankAcquiringNet.Multisplit.Shops` implementation with OAuth authorization, shop registration, shop lookup, banking-detail update models, typed API errors, and local HTTP mock tests.

## 0.1.0-preview.2

### Added

- Added GitHub Actions workflows for CI, release creation, NuGet publishing, and GitHub Packages publishing.
- Added repository badges, package badges, quick start, and richer package documentation to `README.md`.
- Added `LICENSE`.

## 0.1.0-preview.1

### Added

- Added repository skeleton targeting `net10.0`.
- Added package projects:
  - `TBankAcquiringNet`
  - `TBankAcquiringNet.Multisplit.Shops`
  - `TBankAcquiringNet.Multisplit.Payouts`
- Added copied T-Bank integration reference docs for payments, multisplit shop registration, and payouts.
- Added `TBankAcquiringNet` client support for:
  - `Init`
  - `GetState`
  - `CheckOrder`
  - `Cancel`
  - `Confirm`
  - `GetQr`
  - `ChargeQr`
- Added automatic SHA-256 `Token` generation for signed payment requests.
- Added payment notification model and token validation helper.
- Added typed payment status enum and T-Bank wire-value converters.
- Added known payment/QR error-code helper with Russian descriptions.
- Added SDK exception model for transport, protocol, validation, and strict API errors.
- Added optional strict mode for `Success=false` T-Bank responses.
- Added response metadata and optional raw response-body capture.
- Added conservative local request validation.
- Added unit tests for signing, serialization, client request shape, notifications, error handling, metadata, validation, and error-code helpers.
- Added real integration test project gated by environment variables.
- Added release process, testing, error-handling, payments design, and roadmap documentation.
