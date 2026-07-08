# Roadmap

This roadmap is a living implementation checklist. Keep it current whenever public API, test coverage, or package boundaries change.

## Current Focus: Payments

- [x] Create repository skeleton.
- [x] Add `TBankAcquiringNet`.
- [x] Add automatic SHA-256 `Token` generation and verification helper.
- [x] Add `InitAsync`.
- [x] Add `GetStateAsync`.
- [x] Add `CheckOrderAsync`.
- [x] Split unit tests and real integration tests.
- [x] Add `CancelAsync`.
- [x] Add `ConfirmAsync`.
- [x] Add `GetQrAsync`.
- [x] Add `ChargeQrAsync`.
- [x] Add payment notification models and token validation helpers.
- [x] Add documented SDK exception types and error-handling behavior.
- [x] Add optional strict mode for `Success == false` T-Bank responses.
- [x] Add response metadata with optional raw response-body capture.
- [x] Add stronger request validation for required fields and amount minimums.
- [x] Add more documented T-Bank statuses and error-code handling.
- [x] Run real test-environment `Init -> CheckOrder -> GetState -> Cancel -> GetState` with terminal credentials.

Multisplit provider shop registration and payout flows have moved to separate repositories and are no longer tracked here.

## Possible Future Packages

- [ ] `TBankAcquiringNet.AspNetCore` for DI, options binding, webhook endpoint helpers, and health checks.
- [ ] `TBankAcquiringNet.Testing` for fake handlers/builders once app integrations need them.
