<table>
  <tr>
    <td width="170" align="center" valign="middle">
      <img src="https://raw.githubusercontent.com/ai-iskuzhin/TBankAcquiringNet/main/assets/icon.png" width="140" alt="Логотип TBankAcquiringNet" />
    </td>
    <td valign="middle">
      <h1>TBankAcquiringNet</h1>
      <p>Лёгкий .NET SDK для <a href="https://www.tbank.ru/kassa/dev/payments/">эквайринга T-Bank (Тинькофф)</a> без лишних зависимостей — инициализация платежей, проверка статуса, подтверждение, отмена и возврат, QR-операции и проверка платёжных нотификаций.</p>
      <p>
        <a href="https://github.com/ai-iskuzhin/TBankAcquiringNet/actions/workflows/ci.yml"><img src="https://github.com/ai-iskuzhin/TBankAcquiringNet/actions/workflows/ci.yml/badge.svg?branch=main" alt="CI" /></a>
        <a href="https://github.com/ai-iskuzhin/TBankAcquiringNet/actions/workflows/release.yml"><img src="https://github.com/ai-iskuzhin/TBankAcquiringNet/actions/workflows/release.yml/badge.svg" alt="Release" /></a>
        <a href="https://github.com/ai-iskuzhin/TBankAcquiringNet/blob/main/LICENSE"><img src="https://img.shields.io/github/license/ai-iskuzhin/TBankAcquiringNet?style=flat-square" alt="License" /></a>
        <a href="https://dotnet.microsoft.com/"><img src="https://img.shields.io/badge/targets-net10.0-512BD4?logo=dotnet&amp;style=flat-square" alt="Targets" /></a>
      </p>
      <p>
        <a href="https://www.nuget.org/packages/TBankAcquiringNet"><img src="https://img.shields.io/nuget/vpre/TBankAcquiringNet?logo=nuget&amp;style=flat-square" alt="Версия NuGet" /></a>
        <a href="https://www.nuget.org/packages/TBankAcquiringNet"><img src="https://img.shields.io/nuget/dt/TBankAcquiringNet?style=flat-square" alt="Загрузки NuGet" /></a>
      </p>
    </td>
  </tr>
</table>

Типизированная обёртка над HTTP API эквайринга T-Bank. SDK подписывает каждый запрос, десериализует ответы в строго типизированные модели и проверяет входящие платёжные нотификации — без зависимостей сверх BCL. Версионирование следует Semantic Versioning; текущая линейка — preview версии `0.2.0`.

## Установка

```bash
dotnet add package TBankAcquiringNet --prerelease
```

Пакет таргетит `net10.0` и использует встроенный `System.Text.Json`, поэтому не тянет дополнительных зависимостей во время выполнения.

Для локальной разработки можно подключить проект напрямую:

```xml
<ProjectReference Include="src/TBankAcquiringNet/TBankAcquiringNet.csproj" />
```

## Быстрый старт

Возьмите ключ терминала и пароль из личного кабинета мерчанта T-Bank, затем:

```csharp
using TBankAcquiringNet;

using var httpClient = new HttpClient();

var client = new TBankPaymentsClient(httpClient, new TBankPaymentsClientOptions
{
    TerminalKey = "TinkoffBankTest",
    Password = "YOUR_TERMINAL_PASSWORD",
    Environment = TBankAcquiringEnvironment.Test   // или Production
});

var response = await client.InitAsync(new TBankInitPaymentRequest
{
    OrderId = $"order-{Guid.NewGuid():N}",
    Amount = TBankAmount.FromMinorUnits(15000),    // 150.00 RUB
    Description = "Test payment"
});

if (response.Success)
    Console.WriteLine(response.PaymentURL);        // перенаправьте покупателя сюда
else
    Console.WriteLine($"Init failed: {response.ErrorCode} {response.Message}");
```

`TerminalKey` и `Token` запроса заполняются автоматически перед каждым вызовом. Пароль терминала используется локально для вычисления SHA-256 token и никогда не отправляется как поле запроса.

### Среды

`TBankPaymentsClientOptions` определяет базовый адрес по `Environment` (по умолчанию `TBankAcquiringEnvironment.Production`, либо `Test`). Задайте `BaseAddress` явно, чтобы переопределить:

```csharp
new TBankPaymentsClientOptions
{
    TerminalKey = "...",
    Password = "...",
    BaseAddress = new Uri("https://securepay.tinkoff.ru/v2/")
}
```

`Amount` указывается в минимальных единицах валюты (копейках для RUB) через `TBankAmount.FromMinorUnits`.

## Поддерживаемые методы

Каждый вызов возвращает типизированный ответ, производный от `TBankResponse`, с полями `Success`, `ErrorCode`, `Message` и `Details` (см. [Обработка ошибок](#обработка-ошибок)).

| Метод T-Bank | API клиента | Возвращает |
| --- | --- | --- |
| `Init` | `InitAsync` | `TBankInitPaymentResponse` |
| `GetState` | `GetStateAsync` | `TBankPaymentStateResponse` |
| `CheckOrder` | `CheckOrderAsync` | `TBankCheckOrderResponse` |
| `Confirm` | `ConfirmAsync` | `TBankConfirmPaymentResponse` |
| `Cancel` | `CancelAsync` | `TBankCancelPaymentResponse` |
| `GetQr` | `GetQrAsync` | `TBankQrResponse` |
| `ChargeQr` | `ChargeQrAsync` | `TBankChargeQrResponse` |

Пакет также включает автоматическую генерацию token для запросов, проверку token платёжных нотификаций, типизированные статусы платежей, описания известных кодов ошибок T-Bank, метаданные ответа (с опциональным сохранением сырого тела) и консервативную локальную валидацию запросов.

## Двухстадийные платежи

Для двухстадийной схемы авторизуйте платёж методом `Init`, затем спишите удержанную сумму методом `Confirm` после выполнения заказа либо освободите её методом `Cancel`:

```csharp
var confirm = await client.ConfirmAsync(new TBankConfirmPaymentRequest
{
    PaymentId = paymentId,
    Amount = TBankAmount.FromMinorUnits(15000)   // опциональное частичное списание
});
```

`Cancel` покрывает и отмену (до списания), и возврат (после списания); передайте `Amount` для частичного возврата.

## Приём нотификаций

T-Bank отправляет подписанную нотификацию POST-запросом на ваш `NotificationURL`. Проверьте её token перед обработкой, затем верните ровно то тело успеха, которое ожидает API:

```csharp
using TBankAcquiringNet;

// notification: TBankPaymentNotification, разобранный из тела запроса
var result = TBankPaymentNotificationValidator.ValidateToken(notification, password);

if (result != TBankPaymentNotificationValidationResult.Valid)
{
    // result равен MissingToken или InvalidToken — отклоните запрос.
    return Results.BadRequest();
}

// ... обработайте обновление платежа ...

return Results.Text(TBankPaymentNotificationValidator.SuccessResponseBody);   // "OK"
```

Валидатор пересчитывает token из полей нотификации и пароля терминала и сравнивает его с переданным `Token`.

## Обработка ошибок

SDK разделяет *ожидаемые* результаты API и *исключительные* сбои:

- **Ответ T-Bank `Success: false` — это нормальный результат, а не исключение.** Каждый метод возвращает типизированный ответ; читайте `ErrorCode`, `Message`, `Details`, `PaymentId` и `OrderId` для бизнес-решений. Для штатных отказов `try/catch` не нужен.
- **Выбрасываются только сбои, которые мешают получить корректный ответ**, и все они производны от `TBankAcquiringException`:
  - `TBankAcquiringTransportException` — сеть, DNS, TLS или таймаут до получения ответа.
  - `TBankAcquiringProtocolException` — ответ получен, но не может быть разобран (пустое тело, неожиданный JSON, HTML-страница ошибки); содержит `HttpStatusCode` и отредактированный `ResponseBodyPreview`.
  - `TBankAcquiringValidationException` — запрос отклонён локальной валидацией до отправки.

```csharp
try
{
    var state = await client.GetStateAsync(new TBankPaymentStateRequest { PaymentId = paymentId });

    if (state.Success)
        Console.WriteLine(state.Status);
    else
        Console.WriteLine($"Ошибка API: {state.ErrorCode}");   // ожидаемый бизнес-отказ
}
catch (TBankAcquiringTransportException ex)
{
    // Ответ не получен (сбой сети/транспорта).
    Console.WriteLine(ex.Message);
}
catch (TBankAcquiringProtocolException ex)
{
    // Ответ получен, но непригоден.
    Console.WriteLine(ex.HttpStatusCode);
    Console.WriteLine(ex.ResponseBodyPreview);
}
```

Установите `ThrowOnTBankApiError = true`, чтобы вместо этого выбрасывалось `TBankAcquiringApiException` (с `ErrorCode`, `ErrorMessage`, `Details` и `HttpStatusCode`) при любом ответе T-Bank с `Success: false`. Отменённый `CancellationToken` всегда приходит как стандартный `OperationCanceledException`. Полная политика — в [docs/error-handling.md](https://github.com/ai-iskuzhin/TBankAcquiringNet/blob/main/docs/error-handling.md).

## Внедрение зависимостей

Клиент принимает `HttpClient` от вызывающего кода, поэтому интегрируется с `IHttpClientFactory`:

```csharp
services.AddHttpClient("tbank-acquiring");

services.AddSingleton(sp =>
{
    var httpClient = sp.GetRequiredService<IHttpClientFactory>().CreateClient("tbank-acquiring");
    return new TBankPaymentsClient(httpClient, new TBankPaymentsClientOptions
    {
        TerminalKey = configuration["TBank:TerminalKey"]!,
        Password = configuration["TBank:Password"]!,
        Environment = TBankAcquiringEnvironment.Production
    });
});
```

Если вы передаёте собственный `HttpClient`, SDK его не освобождает.

## Разработка

Запустить все тесты:

```bash
dotnet test TBankAcquiringNet.slnx
```

Собрать NuGet-пакет:

```bash
dotnet pack src/TBankAcquiringNet/TBankAcquiringNet.csproj --configuration Release --output artifacts/packages
```

Реальные интеграционные тесты включаются через переменные окружения:

```bash
export TBANK_ACQUIRING_TEST_TERMINAL_KEY="..."
export TBANK_ACQUIRING_TEST_PASSWORD="..."
export TBANK_ACQUIRING_TEST_BASE_URL="https://securepay.tinkoff.ru/v2/"
dotnet test tests/TBankAcquiringNet.Tests.Integration/TBankAcquiringNet.Tests.Integration.csproj
```

## Документация

- [Дизайн Payments](https://github.com/ai-iskuzhin/TBankAcquiringNet/blob/main/docs/payments-design.md)
- [Обработка ошибок](https://github.com/ai-iskuzhin/TBankAcquiringNet/blob/main/docs/error-handling.md)
- [Тестирование](https://github.com/ai-iskuzhin/TBankAcquiringNet/blob/main/docs/testing.md)
- [Roadmap](https://github.com/ai-iskuzhin/TBankAcquiringNet/blob/main/docs/roadmap.md)
- [Процесс релиза](https://github.com/ai-iskuzhin/TBankAcquiringNet/blob/main/docs/release.md)
- [Changelog](https://github.com/ai-iskuzhin/TBankAcquiringNet/blob/main/CHANGELOG.md)

## Лицензия

[MIT](LICENSE)
