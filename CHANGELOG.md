# Changelog

All notable changes to `TBankAcquiringNet` will be documented in this file.

The project uses Semantic Versioning. Versions below `1.0.0` are preview releases and may include public API changes while the SDK contracts are validated against real integrations.

## Unreleased

No changes yet.

## 0.1.0-preview.1

### Added

- Added repository skeleton targeting `net10.0`.
- Added package projects:
  - `TBankAcquiringNet.Payments`
  - `TBankAcquiringNet.Multisplit.Shops`
  - `TBankAcquiringNet.Multisplit.Payouts`
- Added copied T-Bank integration reference docs for payments, multisplit shop registration, and payouts.
- Added `TBankAcquiringNet.Payments` client support for:
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
