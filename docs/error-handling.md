# Error Handling

This SDK should distinguish between:

1. T-Bank business/API responses that arrived successfully.
2. HTTP, network, timeout, serialization, and SDK misuse failures.

## Proposed Default

Client methods should return typed response models for T-Bank API responses, even when T-Bank returns:

```json
{
  "Success": false,
  "ErrorCode": "204",
  "Message": "Invalid token",
  "Details": "..."
}
```

That means regular payment-flow code can inspect:

```csharp
var response = await client.InitAsync(request);

if (!response.Success)
{
    // Persist/handle T-Bank error code and message.
}
```

The SDK should throw exceptions for failures where no trustworthy T-Bank response model is available:

- network/DNS/TLS errors
- request timeout or cancellation
- non-JSON response body
- malformed JSON response
- unexpected non-success HTTP status without a parseable T-Bank body
- invalid SDK configuration
- invalid request object before sending

Request validation failures throw `TBankAcquiringValidationException` before token generation or HTTP sending.

## Exception Types

The SDK exposes SDK-specific exception types instead of leaking raw transport details as the only public shape:

```csharp
public abstract class TBankAcquiringException : Exception;
public sealed class TBankAcquiringTransportException : TBankAcquiringException;
public sealed class TBankAcquiringProtocolException : TBankAcquiringException;
public sealed class TBankAcquiringValidationException : TBankAcquiringException;
```

For non-2xx HTTP responses, the client should first try to parse the body as a T-Bank response. If parsing succeeds, return it. If parsing fails, throw `TBankAcquiringProtocolException` with HTTP status and a redacted response-body preview.

## Optional Strict Mode

Some consumers prefer exceptions for `Success == false`. This is supported as an explicit option, not the default:

```csharp
public bool ThrowOnTBankApiError { get; init; }
```

When enabled, `Success == false` responses throw:

```csharp
public sealed class TBankAcquiringApiException : TBankAcquiringException
{
    public string? ErrorCode { get; }
    public string? ErrorMessage { get; }
    public string? Details { get; }
    public HttpStatusCode? HttpStatusCode { get; }
}
```

## Response Metadata And Raw Payloads

The SDK should expose response metadata for tracing and support, but it should not log or expose raw payloads casually.

Implemented shape:

```csharp
public sealed class TBankAcquiringResponseMetadata
{
    public HttpStatusCode HttpStatusCode { get; }
    public IReadOnlyDictionary<string, string[]> Headers { get; }
    public string? RawResponseBody { get; }
}
```

Typed response models can carry optional metadata:

```csharp
public abstract record TBankResponse
{
    public bool Success { get; init; }
    public string? ErrorCode { get; init; }
    public string? Message { get; init; }
    public string? Details { get; init; }
    public TBankAcquiringResponseMetadata? Metadata { get; init; }
}
```

Default behavior includes status and headers, but not raw response bodies:

```csharp
public bool CaptureRawResponseBody { get; init; }
```

When raw capture is enabled, the SDK stores the exact response body string returned by T-Bank. This helps debug cases where:

- T-Bank returns undocumented fields
- field types differ from documentation
- support asks for the response body
- deserialization fails and the typed model is incomplete

Raw response capture must be opt-in because payment APIs can contain personal or sensitive data such as masked PAN, email, phone, order data, identifiers, and generated tokens.

For protocol exceptions, the SDK may include a short redacted response-body preview even when full raw capture is disabled.

## Why This Shape

T-Bank often reports operation-level failures inside a normal JSON response with `Success`, `ErrorCode`, `Message`, and `Details`. Returning that model avoids turning expected payment outcomes into exceptional control flow.

Transport and protocol failures are different: the SDK cannot reliably tell whether T-Bank accepted, rejected, or even received the request. Those should be exceptions so callers can retry, reconcile with `GetState`, or alert operators.

## Retry Guidance

The SDK should not automatically retry mutating payment methods by default.

Safe default:

- no automatic retries for `Init`, `Confirm`, `Cancel`, `ChargeQr`
- caller-managed retries only with idempotency fields where T-Bank supports them, such as `ExternalRequestId` on `Cancel`
- allow app code to use `HttpClient` policies outside the SDK if it accepts the idempotency tradeoff

When a transport exception occurs after sending a request, application code should reconcile by querying state when possible:

```text
Init uncertain       -> CheckOrder(OrderId)
Confirm uncertain   -> GetState(PaymentId)
Cancel uncertain    -> GetState(PaymentId)
ChargeQr uncertain  -> GetState(PaymentId)
```

## Logging And Redaction

Exceptions and diagnostics must never include:

- terminal password
- generated token
- bearer token
- card data
- CVV
- private keys or certificates

Redacted request/response previews are acceptable for non-secret fields such as `PaymentId`, `OrderId`, `ErrorCode`, and `Status`.
