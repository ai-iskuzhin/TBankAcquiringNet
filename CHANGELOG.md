# Changelog

All notable changes to `TBankAcquiringNet` will be documented in this file.

The project uses Semantic Versioning. Versions below `1.0.0` are preview releases and may include public API changes while the SDK contracts are validated against real integrations.

## Unreleased

No changes yet.

## 0.2.0-preview.4

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
