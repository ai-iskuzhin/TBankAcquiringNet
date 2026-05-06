# TBankAcquiringNet

[![CI](https://github.com/ai-iskuzhin/TBankAcquiringNet/actions/workflows/ci.yml/badge.svg?branch=main)](https://github.com/ai-iskuzhin/TBankAcquiringNet/actions/workflows/ci.yml)
[![Release](https://github.com/ai-iskuzhin/TBankAcquiringNet/actions/workflows/release.yml/badge.svg)](https://github.com/ai-iskuzhin/TBankAcquiringNet/actions/workflows/release.yml)
[![Publish NuGet](https://github.com/ai-iskuzhin/TBankAcquiringNet/actions/workflows/publish-nuget.yml/badge.svg)](https://github.com/ai-iskuzhin/TBankAcquiringNet/actions/workflows/publish-nuget.yml)
[![Publish GitHub Packages](https://github.com/ai-iskuzhin/TBankAcquiringNet/actions/workflows/publish-github-packages.yml/badge.svg)](https://github.com/ai-iskuzhin/TBankAcquiringNet/actions/workflows/publish-github-packages.yml)
[![License](https://img.shields.io/github/license/ai-iskuzhin/TBankAcquiringNet?style=flat-square)](https://github.com/ai-iskuzhin/TBankAcquiringNet/blob/main/LICENSE)
[![.NET](https://img.shields.io/badge/.NET-10.0-512BD4?logo=dotnet&style=flat-square)](https://dotnet.microsoft.com/)
[![Coverage](https://img.shields.io/badge/coverage-not%20published-lightgrey?style=flat-square)](#development)

| Package | Latest Release | Downloads |
| :--- | :---: | :---: |
| `TBankAcquiringNet.Payments` | [![TBankAcquiringNet.Payments NuGet](https://img.shields.io/nuget/vpre/TBankAcquiringNet.Payments?logo=nuget&style=flat-square)](https://www.nuget.org/packages/TBankAcquiringNet.Payments) | [![TBankAcquiringNet.Payments Downloads](https://img.shields.io/nuget/dt/TBankAcquiringNet.Payments?style=flat-square)](https://www.nuget.org/packages/TBankAcquiringNet.Payments) |
| `TBankAcquiringNet.Multisplit.Shops` | planned | planned |
| `TBankAcquiringNet.Multisplit.Payouts` | planned | planned |

Dependency-light .NET SDK libraries for T-Bank acquiring integrations.

Current status: the latest preview is `0.1.0-preview.2`. `TBankAcquiringNet.Payments` is the first implemented package. Multisplit shop registration and payouts are planned as separate packages and are intentionally not packable yet.

## Installation

Payments:

```bash
dotnet add package TBankAcquiringNet.Payments --prerelease
```

For local development, reference the project directly:

```xml
<ProjectReference Include="src/TBankAcquiringNet.Payments/TBankAcquiringNet.Payments.csproj" />
```

## Quick Start

```csharp
using TBankAcquiringNet.Payments;

using var httpClient = new HttpClient();

var client = new TBankPaymentsClient(httpClient, new TBankPaymentsClientOptions
{
    TerminalKey = "TinkoffBankTest",
    Password = "...",
    BaseAddress = new Uri("https://securepay.tinkoff.ru/v2/")
});

var response = await client.InitAsync(new TBankInitPaymentRequest
{
    OrderId = $"order-{Guid.NewGuid():N}",
    Amount = TBankAmount.FromMinorUnits(15000),
    Description = "Test payment"
});

Console.WriteLine(response.PaymentId);
Console.WriteLine(response.PaymentURL);
```

`TerminalKey` and `Token` are filled automatically before requests are sent. The terminal password is used locally for SHA-256 token generation and is not sent as a request field.

## Payments

`TBankAcquiringNet.Payments` currently supports:

| Method | Client API |
| --- | --- |
| `Init` | `InitAsync` |
| `GetState` | `GetStateAsync` |
| `CheckOrder` | `CheckOrderAsync` |
| `Cancel` | `CancelAsync` |
| `Confirm` | `ConfirmAsync` |
| `GetQr` | `GetQrAsync` |
| `ChargeQr` | `ChargeQrAsync` |

The package also includes:

- automatic request token generation
- notification token validation
- typed response models and payment statuses
- known T-Bank error-code descriptions
- optional strict API-error exceptions
- response metadata with optional raw response-body capture
- conservative local request validation

## Notifications

Use `TBankPaymentNotificationValidator` for webhook token checks:

```csharp
var result = TBankPaymentNotificationValidator.Validate(notification, password);

if (!result.IsValid)
{
    return Results.BadRequest();
}

return Results.Text(TBankPaymentNotificationValidator.SuccessResponseBody);
```

## Error Handling

By default, T-Bank API errors such as `Success=false` are returned as typed responses, because callers often need `ErrorCode`, `Message`, `Details`, `PaymentId`, and `OrderId` for business decisions.

Transport, malformed JSON, and local validation failures throw SDK exceptions:

```csharp
try
{
    var state = await client.GetStateAsync(new TBankPaymentStateRequest
    {
        PaymentId = paymentId
    });
}
catch (TBankAcquiringTransportException ex)
{
    // Network, DNS, timeout, or transport-level HTTP client failure.
}
catch (TBankAcquiringProtocolException ex)
{
    // Unexpected or invalid protocol response.
}
catch (TBankAcquiringValidationException ex)
{
    // Invalid local request before sending it to T-Bank.
}
```

Set `ThrowOnTBankApiError = true` to throw `TBankAcquiringApiException` for `Success=false` responses.

See [docs/error-handling.md](https://github.com/ai-iskuzhin/TBankAcquiringNet/blob/main/docs/error-handling.md) for the full policy.

## Package Boundaries

`TBankAcquiringNet.Payments` covers regular acquiring payment flows such as payment initialization, status checks, confirmation, cancellation, QR operations, and payment notifications.

`TBankAcquiringNet.Multisplit.Shops` will cover multisplit provider shop registration and updates, including registration authorization, `shopCode` lookup, and payout banking details.

`TBankAcquiringNet.Multisplit.Payouts` will cover multisplit payout flows, including card, SBP, and partner payouts.

Framework-specific integration packages such as `TBankAcquiringNet.AspNetCore` should be added later only if the SDK needs ASP.NET Core-specific DI, options binding, webhook endpoints, or health checks.

## Documentation

- [Payments design](https://github.com/ai-iskuzhin/TBankAcquiringNet/blob/main/docs/payments-design.md)
- [Error handling](https://github.com/ai-iskuzhin/TBankAcquiringNet/blob/main/docs/error-handling.md)
- [Testing](https://github.com/ai-iskuzhin/TBankAcquiringNet/blob/main/docs/testing.md)
- [Roadmap](https://github.com/ai-iskuzhin/TBankAcquiringNet/blob/main/docs/roadmap.md)
- [Release process](https://github.com/ai-iskuzhin/TBankAcquiringNet/blob/main/docs/release.md)

Copied integration reference notes live in:

```text
docs/integrations/t-bank-acquiring/oplata_multisplit.md
docs/integrations/t-bank-acquiring/api_reg_upd_multisplit.md
docs/integrations/t-bank-acquiring/vyplaty-multisplit.md
```

## Development

Run all tests:

```bash
dotnet test TBankAcquiringNet.slnx
```

Pack Payments:

```bash
dotnet pack src/TBankAcquiringNet.Payments/TBankAcquiringNet.Payments.csproj --configuration Release --output artifacts/packages
```

Real integration tests are gated by environment variables:

```bash
export TBANK_ACQUIRING_TEST_TERMINAL_KEY="..."
export TBANK_ACQUIRING_TEST_PASSWORD="..."
export TBANK_ACQUIRING_TEST_BASE_URL="https://securepay.tinkoff.ru/v2/"
dotnet test tests/TBankAcquiringNet.Payments.Tests.Integration/TBankAcquiringNet.Payments.Tests.Integration.csproj
```
