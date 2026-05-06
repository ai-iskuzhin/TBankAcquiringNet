# TBankAcquiringNet

Dependency-light .NET SDK libraries for T-Bank acquiring integrations.

The repository uses the same product-style name as the .NET solution and package root:

```text
TBankAcquiringNet
```

The .NET solution, projects, namespaces, and NuGet package identities use the same PascalCase root:

```text
TBankAcquiringNet
```

## Projects

```text
src/TBankAcquiringNet.Payments
src/TBankAcquiringNet.Multisplit.Shops
src/TBankAcquiringNet.Multisplit.Payouts
```

## Reference Docs

The current source notes for the three integration directions are copied into:

```text
docs/integrations/t-bank-acquiring/oplata_multisplit.md
docs/integrations/t-bank-acquiring/api_reg_upd_multisplit.md
docs/integrations/t-bank-acquiring/vyplaty-multisplit.md
```

The first proposed implementation slice is documented in:

```text
docs/payments-design.md
```

Testing and integration-test environment variables are documented in:

```text
docs/testing.md
```

The implementation roadmap is tracked in:

```text
docs/roadmap.md
```

Error-handling policy is tracked in:

```text
docs/error-handling.md
```

Release process is tracked in:

```text
docs/release.md
```

## Package Boundaries

`TBankAcquiringNet.Payments` covers regular acquiring payment flows such as payment initialization, status checks, confirmation, cancellation, refunds, and payment notifications.

`TBankAcquiringNet.Multisplit.Shops` covers multisplit provider shop registration and updates, including registration authorization, `shopCode` lookup, and payout banking details.

`TBankAcquiringNet.Multisplit.Payouts` covers multisplit payout flows, including card, SBP, and partner payouts.

Framework-specific integration packages such as `TBankAcquiringNet.AspNetCore` should be added later only if the SDK needs ASP.NET Core-specific DI, options binding, webhook endpoints, or health checks.
