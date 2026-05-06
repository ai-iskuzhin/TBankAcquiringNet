# Roadmap

This roadmap is a living implementation checklist. Keep it current whenever public API, test coverage, or package boundaries change.

## Current Focus: Payments

- [x] Create repository skeleton and package split.
- [x] Add copied T-Bank integration reference docs.
- [x] Add `TBankAcquiringNet.Payments`.
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

## Next Package: Multisplit Shops

- [ ] Make `TBankAcquiringNet.Multisplit.Shops` packable when the public SDK surface exists.
- [ ] Add OAuth token client.
- [ ] Add provider shop registration request/response models.
- [ ] Add shop lookup by `shopCode`.
- [ ] Add provider banking details update.
- [ ] Add integration tests gated by registration credentials and mTLS requirements.

## Later Package: Multisplit Payouts

- [ ] Make `TBankAcquiringNet.Multisplit.Payouts` packable when the public SDK surface exists.
- [ ] Add payout `Init`.
- [ ] Add payout status lookup.
- [ ] Add token-signing mode for payouts.
- [ ] Add certificate signature model support for `DigestValue`, `SignatureValue`, and `X509SerialNumber`.
- [ ] Add card/SBP/partner payout request shapes.

## Possible Future Packages

- [ ] `TBankAcquiringNet.AspNetCore` for DI, options binding, webhook endpoint helpers, and health checks.
- [ ] `TBankAcquiringNet.Testing` for fake handlers/builders once app integrations need them.
