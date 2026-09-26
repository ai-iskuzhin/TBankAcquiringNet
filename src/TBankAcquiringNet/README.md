# TBankAcquiringNet

.NET SDK для платёжных сценариев эквайринга T-Bank. Поддерживает `netstandard2.0`, `net8.0` и `net10.0` (.NET Framework 4.6.2+, .NET Core 2.0+, Mono, Unity).

> [!WARNING]
> **Обновление обязательно для работы с T-API.** T-Bank перевёл `securepay.tinkoff.ru` и `rest-api-test.tinkoff.ru` на TLS-сертификаты Национального удостоверяющего центра Минцифры России (Russian Trusted CA). Их корень отсутствует в доверенных хранилищах большинства ОС, рантаймов и контейнерных образов, поэтому **все запросы к T-API падают** с `AuthenticationException` / `PartialChain` / `certificate verify failed` / `unable to get local issuer certificate` / `PKIX path building failed`.
>
> Сертификаты Минцифры встроены в пакет начиная с 1.5.0. Одного обновления **недостаточно** — создавайте транспорт через `TBankHttpClientFactory`, иначе `new HttpClient()` по-прежнему не будет доверять цепочке.

## Установка

```bash
dotnet add package TBankAcquiringNet
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
- T‑Pay и SberPay: `GetTinkoffPayStatus`, `GetTinkoffPayLink`, `GetTinkoffPayQr`, `GetSberPayQr`, `GetSberPayLink`
- Mir Pay и Alfa Pay: `GetMirPayDeepLink`, `GetAlfaPayLink`
- чеки: `SendClosingReceipt` (фискализация, ФФД 1.2 и 1.05)
- покупатели и карты: `AddCustomer`, `GetCustomer`, `RemoveCustomer`, `AddCard`, `GetAddCardState`, `GetCardList`, `RemoveCard`
- рекуррентные платежи: `Charge` (по сохранённому `RebillId`)
- уведомления о статусе платежа
- уведомления о привязке счёта
- генерация SHA-256 `Token` и проверка уведомлений

## Быстрый старт

```csharp
using TBankAcquiringNet;

// Доверяет корням Минцифры России, которыми подписан TLS-сертификат T-API.
using var httpClient = TBankHttpClientFactory.CreateHttpClient();

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

Передайте тело запроса в `TryRead`: он разберёт уведомление и проверит подпись за один проход.

```csharp
var body = await new StreamReader(Request.Body).ReadToEndAsync();
var result = TBankPaymentNotificationValidator.TryRead(body, password, out var notification);

if (result != TBankPaymentNotificationValidationResult.Valid)
{
    // MissingToken, InvalidToken или MalformedBody
    return Results.BadRequest();
}

return Results.Text(TBankPaymentNotificationValidator.SuccessResponseBody);
```

Подпись считается по полям так, как их прислал банк, а не по разобранной модели: поле, которого SDK
ещё не знает, участвует в подписи и доступно в `notification.AdditionalFields`, а статус, которого нет
в `TBankPaymentStatus`, не роняет разбор — он останется строкой в `notification.StatusRaw`.

## Обработка ошибок

Ответы API с `Success=false` по умолчанию возвращаются как типизированные модели, чтобы вызывающий код мог прочитать `ErrorCode`, `Message`, `Details`, `PaymentId` и `OrderId`.

Транспортные, протокольные и локальные ошибки валидации выбрасываются как исключения SDK.

## TLS-сертификаты Минцифры

TLS-сертификат T-API выпущен Национальным удостоверяющим центром Минцифры России, а не публичным CA. Корневой и промежуточные сертификаты встроены в пакет; подключите их через транспорт:

```csharp
using var httpClient = TBankHttpClientFactory.CreateHttpClient();
```

Для `IHttpClientFactory`:

```csharp
services
    .AddHttpClient("tbank-acquiring")
    .ConfigurePrimaryHttpMessageHandler(TBankHttpClientFactory.CreateHandler);
```

Проверка сертификата не ослабляется: сначала работает штатная проверка платформы, встроенные корни задействуются только при ошибке цепочки. Несовпадение имени хоста, истёкший сертификат и посторонние центры сертификации по-прежнему отклоняются. Альтернатива — установить корень Минцифры в системное хранилище.

Встроена только RSA-цепочка: .NET не поддерживает ГОСТ-подписи и ГОСТ-шифронаборы TLS. Подробности — в [docs/tls-certificates.md](https://github.com/ai-iskuzhin/TBankAcquiringNet/blob/main/docs/tls-certificates.md).

## Репозиторий

Исходный код, трекер задач и полная документация — в репозитории:

https://github.com/ai-iskuzhin/TBankAcquiringNet
