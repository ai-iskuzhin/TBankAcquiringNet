<table>
  <tr>
    <td width="170" align="center" valign="middle">
      <img src="https://raw.githubusercontent.com/ai-iskuzhin/TBankAcquiringNet/main/assets/icon.png" width="140" alt="Логотип TBankAcquiringNet" />
    </td>
    <td valign="middle">
      <h1>TBankAcquiringNet</h1>
      <p>.NET SDK для <a href="https://www.tbank.ru/kassa/dev/payments/">эквайринга T-Bank (Тинькофф)</a> — инициализация платежей, проверка статуса, подтверждение, отмена и возврат, QR-операции и проверка платёжных уведомлений.</p>
      <p>
        <a href="https://github.com/ai-iskuzhin/TBankAcquiringNet/actions/workflows/ci.yml"><img src="https://github.com/ai-iskuzhin/TBankAcquiringNet/actions/workflows/ci.yml/badge.svg?branch=main" alt="CI" /></a>
        <a href="https://github.com/ai-iskuzhin/TBankAcquiringNet/actions/workflows/release.yml"><img src="https://github.com/ai-iskuzhin/TBankAcquiringNet/actions/workflows/release.yml/badge.svg" alt="Release" /></a>
        <a href="https://github.com/ai-iskuzhin/TBankAcquiringNet/blob/main/LICENSE"><img src="https://img.shields.io/github/license/ai-iskuzhin/TBankAcquiringNet?style=flat-square" alt="License" /></a>
        <a href="https://dotnet.microsoft.com/"><img src="https://img.shields.io/badge/targets-netstandard2.0%20%7C%20net8.0%20%7C%20net10.0-512BD4?logo=dotnet&amp;style=flat-square" alt="Targets" /></a>
      </p>
      <p>
        <a href="https://www.nuget.org/packages/TBankAcquiringNet"><img src="https://img.shields.io/nuget/v/TBankAcquiringNet?logo=nuget&amp;style=flat-square" alt="Версия NuGet" /></a>
        <a href="https://www.nuget.org/packages/TBankAcquiringNet"><img src="https://img.shields.io/nuget/dt/TBankAcquiringNet?style=flat-square" alt="Загрузки NuGet" /></a>
      </p>
    </td>
  </tr>
</table>

## Установка

```bash
dotnet add package TBankAcquiringNet
```

Проект поддерживает `netstandard2.0`, `net8.0` и `net10.0`. На `net8.0` и `net10.0` используется встроенный `System.Text.Json` без дополнительных зависимостей. Ресурс `netstandard2.0` подтягивает пакеты `System.Text.Json` и `System.Net.Http.Json` и работает на .NET Framework 4.6.2+, .NET Core 2.0+, Mono и Unity.

Для локальной разработки можно подключить проект напрямую:

```xml
<ProjectReference Include="src/TBankAcquiringNet/TBankAcquiringNet.csproj" />
```

### Использование на .NET Framework

На .NET Framework транзитивные зависимости `System.Text.Json` могут требовать binding redirects. В проектах в формате `packages.config` (или без автогенерации) включите их:

```xml
<PropertyGroup>
  <AutoGenerateBindingRedirects>true</AutoGenerateBindingRedirects>
</PropertyGroup>
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

Покрытие методов [Эквайринг API Т‑Бизнес](https://developer.tbank.ru/eacq/api/priem-platezhei). В колонке «SDK API» указан метод клиента; «—» означает, что метод пока не реализован. Ответы (кроме SVG QR) типизированы и производны от `TBankResponse` с полями `Success`, `ErrorCode`, `Message`, `Details` (см. [Обработка ошибок](#обработка-ошибок)).

### Проведение платежа

| Метод T-Bank | SDK API | Документация |
| --- | --- | --- |
| Инициировать платеж | `InitAsync` | [init](https://developer.tbank.ru/eacq/api/init) |
| Подтвердить платеж (FinishAuthorize) | — | [finish-authorize](https://developer.tbank.ru/eacq/api/finish-authorize) |
| Подтвердить списание (Confirm) | `ConfirmAsync` | [confirm](https://developer.tbank.ru/eacq/api/confirm) |

### Методы для работы с 3DS

| Метод T-Bank | SDK API | Документация |
| --- | --- | --- |
| Проверить версию 3DS | — | [check-3-ds-version](https://developer.tbank.ru/eacq/api/check-3-ds-version) |
| Пройти этап 3DS Method | — | [3-ds-method](https://developer.tbank.ru/eacq/api/3-ds-method) |
| Отправить запрос в банк-эмитент для прохождения 3DS | — | [acs-url](https://developer.tbank.ru/eacq/api/acs-url) |
| Подтвердить прохождение 3DS v1.0 | — | [submit-3-ds-authorization](https://developer.tbank.ru/eacq/api/submit-3-ds-authorization) |
| Подтвердить прохождение 3DS v2.1 | — | [submit-3-ds-authorization-v-2](https://developer.tbank.ru/eacq/api/submit-3-ds-authorization-v-2) |

### СБП

| Метод T-Bank | SDK API | Документация |
| --- | --- | --- |
| Сформировать QR | `GetQrAsync` | [get-qr](https://developer.tbank.ru/eacq/api/get-qr) |
| Получить список банков-пользователей QR для возврата | `QrMembersListAsync` | [qr-members-list](https://developer.tbank.ru/eacq/api/qr-members-list) |
| Привязать счет к магазину | `AddAccountQrAsync` | [add-account-qr](https://developer.tbank.ru/eacq/api/add-account-qr) |
| Получить статус привязки счета к магазину | `GetAddAccountQrStateAsync` | [get-add-account-qr-state](https://developer.tbank.ru/eacq/api/get-add-account-qr-state) |
| Получить список счетов, привязанных к магазину | `GetAccountQrListAsync` | [get-account-qr-list](https://developer.tbank.ru/eacq/api/get-account-qr-list) |
| Создать тестовую платежную сессию | `SbpPayTestAsync` | [sbp-pay-test](https://developer.tbank.ru/eacq/api/sbp-pay-test) |
| Получить статус возврата | `GetQrStateAsync` | [get-qr-state](https://developer.tbank.ru/eacq/api/get-qr-state) |
| Получить список банков-участников СБП для платежа | `GetQrBankListAsync` | [get-qr-bank-list](https://developer.tbank.ru/eacq/api/get-qr-bank-list) |

### T‑Pay

| Метод T-Bank | SDK API | Документация |
| --- | --- | --- |
| Определить возможность проведения платежа | `GetTinkoffPayStatusAsync` | [status](https://developer.tbank.ru/eacq/api/status) |
| Получить ссылку | `GetTinkoffPayLinkAsync` | [link](https://developer.tbank.ru/eacq/api/link) |
| Получить QR | `GetTinkoffPayQrAsync` | [qr](https://developer.tbank.ru/eacq/api/qr) |

### SberPay

| Метод T-Bank | SDK API | Документация |
| --- | --- | --- |
| Получить QR | `GetSberPayQrAsync` | [sber-pay-qr](https://developer.tbank.ru/eacq/api/sber-pay-qr) |
| Получить ссылку | `GetSberPayLinkAsync` | [sber-paylink](https://developer.tbank.ru/eacq/api/sber-paylink) |

### Mir Pay и Alfa Pay

| Метод T-Bank | SDK API | Документация |
| --- | --- | --- |
| Mir Pay — Получить DeepLink | `GetMirPayDeepLinkAsync` | [get-deep-link](https://developer.tbank.ru/eacq/api/get-deep-link) |
| Alfa Pay — Получить ссылку | `GetAlfaPayLinkAsync` | [alfa-pay](https://developer.tbank.ru/eacq/api/alfa-pay) |

### Проведение платежа по сохраненным реквизитам

| Метод T-Bank | SDK API | Документация |
| --- | --- | --- |
| Провести платеж по сохраненным реквизитам | — | [charge](https://developer.tbank.ru/eacq/api/charge) |
| Автоплатеж по QR СБП | `ChargeQrAsync` | [charge-qr](https://developer.tbank.ru/eacq/api/charge-qr) |

### Отмена, статус и справки

| Метод T-Bank | SDK API | Документация |
| --- | --- | --- |
| Отменить платеж | `CancelAsync` | [cancel](https://developer.tbank.ru/eacq/api/cancel) |
| Получить статус платежа | `GetStateAsync` | [get-state](https://developer.tbank.ru/eacq/api/get-state) |
| Получить статус заказа | `CheckOrderAsync` | [check-order](https://developer.tbank.ru/eacq/api/check-order) |
| Получить справку по операции | — | [get-confirm-operation](https://developer.tbank.ru/eacq/api/get-confirm-operation) |

### Привязка карты

| Метод T-Bank | SDK API | Документация |
| --- | --- | --- |
| Зарегистрировать покупателя | — | [add-customer](https://developer.tbank.ru/eacq/api/add-customer) |
| Получить данные покупателя | — | [get-customer](https://developer.tbank.ru/eacq/api/get-customer) |
| Удалить данные покупателя | — | [remove-customer](https://developer.tbank.ru/eacq/api/remove-customer) |
| Инициировать привязку карты к покупателю | — | [add-card](https://developer.tbank.ru/eacq/api/add-card) |
| Привязать карту | — | [attach-card](https://developer.tbank.ru/eacq/api/attach-card) |
| Получить статус привязки карты | — | [get-add-card-state](https://developer.tbank.ru/eacq/api/get-add-card-state) |
| Получить список карт покупателя | — | [get-card-list](https://developer.tbank.ru/eacq/api/get-card-list) |
| Удалить привязанную карту покупателя | — | [remove-card](https://developer.tbank.ru/eacq/api/remove-card) |

### Чеки

| Метод T-Bank | SDK API | Документация |
| --- | --- | --- |
| Отправить закрывающий чек в кассу | `SendClosingReceiptAsync` | [send-closing-receipt](https://developer.tbank.ru/eacq/api/send-closing-receipt) |

Методы T‑Pay и SberPay выполняются как GET-запросы без подписи `Token`; методы QR возвращают SVG-изображение строкой. Методы Mir Pay и Alfa Pay — подписанные POST-запросы.

> **Примечание.** DeepLink Mir Pay открывается только на мобильных устройствах Android — приложение Mir Pay доступно исключительно на Android.

Пакет также включает автоматическую генерацию token для запросов, проверку token платёжных уведомлений, типизированные статусы платежей, описания известных кодов ошибок T-Bank, метаданные ответа (с опциональным сохранением сырого тела) и консервативную локальную валидацию запросов.

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

## Платежи по СБП

Зарегистрируйте QR по платежу и при необходимости проверьте статус возврата:

```csharp
// Динамический QR СБП: payload или SVG-изображение
var qr = await client.GetQrAsync(new TBankQrRequest
{
    PaymentId = paymentId,
    DataType = TBankQrDataType.Payload   // или Image — SVG-картинка
});
Console.WriteLine(qr.Data);

// Статус возврата по СБП
var qrState = await client.GetQrStateAsync(new TBankQrStateRequest { PaymentId = paymentId });
Console.WriteLine(qrState.Status);       // например CONFIRMED, REFUNDED

// Автоплатёж по привязанному счёту СБП
var charge = await client.ChargeQrAsync(new TBankChargeQrRequest
{
    PaymentId = paymentId,
    AccountToken = accountToken          // из GetAccountQrList / уведомления о привязке
});
```

Список банков-участников (`GetQrBankListAsync`), участников возврата (`QrMembersListAsync`) и привязка счетов к магазину (`AddAccountQrAsync`, `GetAddAccountQrStateAsync`, `GetAccountQrListAsync`) вызываются аналогично — см. таблицу выше.

## Кошельки: T‑Pay, SberPay, Mir Pay, Alfa Pay

Кошельки возвращают ссылку для редиректа, deeplink или QR для уже инициированного платежа (`Init`):

```csharp
// T‑Pay: проверьте доступность на терминале, затем получите ссылку
var tpay = await client.GetTinkoffPayStatusAsync();
if (tpay.Params?.Allowed == true)
{
    var link = await client.GetTinkoffPayLinkAsync(paymentId, tpay.Params.Version!);
    Console.WriteLine(link.Params?.RedirectUrl);
}

// Mir Pay: подписанный DeepLink (открывается только на Android)
var mir = await client.GetMirPayDeepLinkAsync(new TBankMirPayDeepLinkRequest { PaymentId = paymentId });
Console.WriteLine(mir.Deeplink);

// Alfa Pay: ссылка для редиректа
var alfa = await client.GetAlfaPayLinkAsync(new TBankAlfaPayLinkRequest { PaymentId = paymentId });
Console.WriteLine(alfa.Params?.RedirectUrl);

// SberPay: QR для десктопа возвращается строкой SVG
string sberQr = await client.GetSberPayQrAsync(paymentId);
```

## Фискализация чеков

Отправьте закрывающий чек в кассу. Модель чека выбирается по версии ФФД: `TBankReceiptFfd12` (ФФД 1.2) или `TBankReceiptFfd105` (ФФД 1.05):

```csharp
await client.SendClosingReceiptAsync(new TBankSendClosingReceiptFfd12Request
{
    PaymentId = paymentId,
    Receipt = new TBankReceiptFfd12
    {
        Taxation = "osn",
        Email = "customer@example.com",
        Items =
        [
            new TBankReceiptItemFfd12
            {
                Name = "Наименование товара",
                Price = 10000,           // копейки
                Quantity = 1m,
                Amount = 10000,          // Price × Quantity
                Tax = "vat10",
                MeasurementUnit = "шт"
            }
        ]
    }
});
```

## Приём уведомлений

T-Bank отправляет подписанное уведомление POST-запросом на ваш `NotificationURL`. Проверьте его token перед обработкой, затем верните ровно то тело успеха, которое ожидает API:

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

Валидатор пересчитывает token из полей уведомления и пароля терминала и сравнивает его с переданным `Token`.

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
