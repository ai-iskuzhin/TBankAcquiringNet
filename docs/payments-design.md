# Payments Package Design

This note captures the proposed starting shape for `TBankAcquiringNet`.

## Goal

Provide a small typed SDK for T-Bank acquiring payment acceptance without bringing in ASP.NET Core, persistence, queues, or Sportgearhub-specific workflow code.

## Initial Surface

Start with one client:

```csharp
public sealed class TBankPaymentsClient
{
    public Task<TBankInitPaymentResponse> InitAsync(TBankInitPaymentRequest request, CancellationToken cancellationToken = default);
    public Task<TBankPaymentStateResponse> GetStateAsync(TBankPaymentStateRequest request, CancellationToken cancellationToken = default);
    public Task<TBankCheckOrderResponse> CheckOrderAsync(TBankCheckOrderRequest request, CancellationToken cancellationToken = default);
    public Task<TBankConfirmPaymentResponse> ConfirmAsync(TBankConfirmPaymentRequest request, CancellationToken cancellationToken = default);
    public Task<TBankCancelPaymentResponse> CancelAsync(TBankCancelPaymentRequest request, CancellationToken cancellationToken = default);
    public Task<TBankQrResponse> GetQrAsync(TBankQrRequest request, CancellationToken cancellationToken = default);
    public Task<TBankChargeQrResponse> ChargeQrAsync(TBankChargeQrRequest request, CancellationToken cancellationToken = default);
}
```

Keep constructor configuration explicit:

```csharp
public sealed class TBankPaymentsClientOptions
{
    public required string TerminalKey { get; init; }
    public required string Password { get; init; }
    public TBankAcquiringEnvironment Environment { get; init; } = TBankAcquiringEnvironment.Production;
    public Uri? BaseAddress { get; init; }
}
```

## Models

Use typed request and response models with T-Bank wire names:

```text
TerminalKey
Amount
OrderId
PaymentId
PaymentURL
Status
Success
ErrorCode
Message
Details
PaymentRecipientId
DealId
CreateDealWithType
LevelOfConfidence
DATA
```

Represent money as minor units:

```csharp
public readonly record struct TBankAmount(long MinorUnits);
```

Preserve IDs as strings unless T-Bank clearly requires numeric operations. Several docs show numeric-looking IDs, but SDK consumers should not lose leading zeroes or exceed numeric ranges accidentally.

## Token Handling

Hash/token generation should be owned by the SDK and applied automatically before sending requests. Consumers should not have to calculate `Token` for normal payment calls.

Add one focused token service:

```csharp
public static class TBankToken
{
    public static string Create<TPayload>(TPayload payload, string password);
    public static bool Verify<TPayload>(TPayload payload, string password, string expectedToken);
}
```

Token generation should:

- include top-level primitive request fields only
- exclude `Token`
- include `Password`
- sort keys alphabetically
- concatenate values
- SHA-256 hash the resulting string
- return the lowercase hex digest

Do not include nested objects such as `DATA` in the generic token input unless a specific method's documentation explicitly says otherwise. The T-Bank examples describe pairs of top-level request/notification parameters.

Client methods should use the token service internally:

```csharp
public async Task<TBankInitPaymentResponse> InitAsync(
    TBankInitPaymentRequest request,
    CancellationToken cancellationToken = default)
{
    var signedRequest = request with
    {
        TerminalKey = options.TerminalKey,
        Token = TBankToken.Create(request, options.Password)
    };

    return await SendAsync<TBankInitPaymentResponse>("Init", signedRequest, cancellationToken);
}
```

Request models should expose `Token` only when useful for diagnostics, testing, or advanced callers:

```csharp
public sealed record TBankInitPaymentRequest
{
    public string? Token { get; init; }
}
```

The default client behavior should overwrite or fill `Token` from configured credentials. If manual token mode is ever needed, make it explicit in options:

```csharp
public bool AutoGenerateToken { get; init; } = true;
```

The implementation must be verified against the T-Bank examples before public release.

## Notifications

Keep notification parsing framework-neutral:

```csharp
public sealed record TBankPaymentNotification(
    string TerminalKey,
    string OrderId,
    string PaymentId,
    TBankPaymentStatus Status,
    bool Success,
    string ErrorCode,
    string Token);
```

Do not add ASP.NET Core webhook endpoints yet. A future `TBankAcquiringNet.AspNetCore` package can wrap these primitives later.

## Endpoint Selection

Default endpoints:

```text
Test:       https://rest-api-test.tinkoff.ru/v2/
Production: https://securepay.tinkoff.ru/v2/
```

Allow `BaseAddress` override for tests, sandboxes, and bank-specific environment differences.

## First Implementation Slice

Recommended order:

1. Client options and environment endpoint selection.
2. Shared JSON serializer settings.
3. `InitAsync`, `GetStateAsync`, and `CheckOrderAsync`.
4. Token generation and tests.
5. Payment status enum/parser.
6. Notification model and token validation.
7. `CancelAsync` and `ConfirmAsync`.
8. `GetQrAsync` and `ChargeQrAsync`.
9. Payment notification validation.
10. Rich fiscal receipt/client models for `Confirm` when online-cash-register scenarios need them.

## Notifications

Payment callbacks are represented by `TBankPaymentNotification` and should be verified before application code trusts status, amount, order, or terminal values:

```csharp
var validation = TBankPaymentNotificationValidator.ValidateToken(notification, password);
if (validation != TBankPaymentNotificationValidationResult.Valid)
{
    // Reject or quarantine the callback.
}
```

After successful processing, webhook handlers should return:

```text
OK
```

The payments package stays framework-neutral. ASP.NET Core endpoint helpers belong in a future adapter package if they become useful.

## Request Validation

The payments client performs conservative local validation before signing or sending a request:

- required payment IDs and order IDs must not be blank
- required QR account tokens must not be blank
- payment amounts must be positive
- documented minimum amount checks use 100 minor units where the payment method does not have a more specific local mode
- `DATA` is limited to 20 pairs
- `ChargeQr` requires `InfoEmail` when `SendEmail` is `true`

Conditional multisplit rules that depend on terminal configuration, payment method, fiscal settings, or bank-side risk settings should remain bank-validated until the SDK models those flows explicitly.

## Error Codes

Known payment/QR error codes from the local T-Bank docs are exposed through `TBankPaymentErrorCodes`.

Use it for lightweight branching and display hints:

```csharp
if (response.ErrorCode == TBankPaymentErrorCodes.InsufficientFunds)
{
    // Недостаточно средств.
}
```

The catalog is intentionally small and documentation-backed. Unknown bank codes should remain available as raw `ErrorCode` values on responses.
