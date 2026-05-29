# TBankAcquiringNet.Payments

Dependency-light .NET SDK package for T-Bank acquiring payment flows.

## Install

```bash
dotnet add package TBankAcquiringNet.Payments --prerelease
```

## Supported API

`TBankAcquiringNet.Payments` currently includes typed support for:

- `Init`
- `GetState`
- `CheckOrder`
- `Cancel`
- `Confirm`
- `GetQr`
- `ChargeQr`
- payment status notifications
- account binding notifications
- SHA-256 `Token` generation and notification validation

## Quick Start

```csharp
using TBankAcquiringNet.Payments;

using var httpClient = new HttpClient();

var client = new TBankPaymentsClient(httpClient, new TBankPaymentsClientOptions
{
    TerminalKey = "TinkoffBankTest",
    Password = "...",
    Environment = TBankAcquiringEnvironment.Test
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

`TerminalKey` and `Token` are filled automatically before payment requests are sent. The terminal password is used locally to generate the request token and is not sent as a request field.

## Notifications

```csharp
var result = TBankPaymentNotificationValidator.Validate(notification, password);

if (!result.IsValid)
{
    return Results.BadRequest();
}

return Results.Text(TBankPaymentNotificationValidator.SuccessResponseBody);
```

## Error Handling

API responses with `Success=false` are returned as typed response models by default so callers can inspect `ErrorCode`, `Message`, `Details`, `PaymentId`, and `OrderId`.

Transport, protocol, and local validation failures are thrown as SDK exceptions.

## Repository

Source, issue tracking, and full documentation live in the repository:

https://github.com/ai-iskuzhin/TBankAcquiringNet
