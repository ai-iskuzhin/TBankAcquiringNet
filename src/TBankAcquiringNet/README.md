# TBankAcquiringNet

Лёгкий .NET SDK для платёжных сценариев эквайринга T-Bank без лишних зависимостей. Поддерживает `netstandard2.0`, `net8.0` и `net10.0`.

## Установка

```bash
dotnet add package TBankAcquiringNet --prerelease
```

## Поддерживаемое API

`TBankAcquiringNet` сейчас включает типизированную поддержку:

- `Init`
- `GetState`
- `CheckOrder`
- `Cancel`
- `Confirm`
- `GetQr`
- `ChargeQr`
- СБП: `GetQrState`, `GetQrBankList`, `QrMembersList`, `AddAccountQr`, `GetAddAccountQrState`, `GetAccountQrList`, `SbpPayTest`
- уведомления о статусе платежа
- уведомления о привязке счёта
- генерация SHA-256 `Token` и проверка уведомлений

## Быстрый старт

```csharp
using TBankAcquiringNet;

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

`TerminalKey` и `Token` заполняются автоматически перед отправкой платёжных запросов. Пароль терминала используется локально для генерации token запроса и не отправляется как поле запроса.

## Уведомления

```csharp
var result = TBankPaymentNotificationValidator.ValidateToken(notification, password);

if (result != TBankPaymentNotificationValidationResult.Valid)
{
    return Results.BadRequest();
}

return Results.Text(TBankPaymentNotificationValidator.SuccessResponseBody);
```

## Обработка ошибок

Ответы API с `Success=false` по умолчанию возвращаются как типизированные модели, чтобы вызывающий код мог прочитать `ErrorCode`, `Message`, `Details`, `PaymentId` и `OrderId`.

Транспортные, протокольные и локальные ошибки валидации выбрасываются как исключения SDK.

## Репозиторий

Исходный код, трекер задач и полная документация — в репозитории:

https://github.com/ai-iskuzhin/TBankAcquiringNet
