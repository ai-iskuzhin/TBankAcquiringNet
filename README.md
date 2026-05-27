# TBankAcquiringNet

[![CI](https://github.com/ai-iskuzhin/TBankAcquiringNet/actions/workflows/ci.yml/badge.svg?branch=main)](https://github.com/ai-iskuzhin/TBankAcquiringNet/actions/workflows/ci.yml)
[![Release](https://github.com/ai-iskuzhin/TBankAcquiringNet/actions/workflows/release.yml/badge.svg)](https://github.com/ai-iskuzhin/TBankAcquiringNet/actions/workflows/release.yml)
[![Publish NuGet](https://github.com/ai-iskuzhin/TBankAcquiringNet/actions/workflows/publish-nuget.yml/badge.svg)](https://github.com/ai-iskuzhin/TBankAcquiringNet/actions/workflows/publish-nuget.yml)
[![Publish GitHub Packages](https://github.com/ai-iskuzhin/TBankAcquiringNet/actions/workflows/publish-github-packages.yml/badge.svg)](https://github.com/ai-iskuzhin/TBankAcquiringNet/actions/workflows/publish-github-packages.yml)
[![License](https://img.shields.io/github/license/ai-iskuzhin/TBankAcquiringNet?style=flat-square)](https://github.com/ai-iskuzhin/TBankAcquiringNet/blob/main/LICENSE)
[![.NET](https://img.shields.io/badge/.NET-10.0-512BD4?logo=dotnet&style=flat-square)](https://dotnet.microsoft.com/)
[![Coverage](https://img.shields.io/badge/coverage-not%20published-lightgrey?style=flat-square)](#разработка)

| Пакет | Последняя версия | Загрузки |
| :--- | :---: | :---: |
| `TBankAcquiringNet.Payments` | [![TBankAcquiringNet.Payments NuGet](https://img.shields.io/nuget/vpre/TBankAcquiringNet.Payments?logo=nuget&style=flat-square)](https://www.nuget.org/packages/TBankAcquiringNet.Payments) | [![TBankAcquiringNet.Payments Downloads](https://img.shields.io/nuget/dt/TBankAcquiringNet.Payments?style=flat-square)](https://www.nuget.org/packages/TBankAcquiringNet.Payments) |
| `TBankAcquiringNet.Multisplit.Shops` | [![TBankAcquiringNet.Multisplit.Shops NuGet](https://img.shields.io/nuget/vpre/TBankAcquiringNet.Multisplit.Shops?logo=nuget&style=flat-square)](https://www.nuget.org/packages/TBankAcquiringNet.Multisplit.Shops) | [![TBankAcquiringNet.Multisplit.Shops Downloads](https://img.shields.io/nuget/dt/TBankAcquiringNet.Multisplit.Shops?style=flat-square)](https://www.nuget.org/packages/TBankAcquiringNet.Multisplit.Shops) |
| `TBankAcquiringNet.Multisplit.Payouts` | планируется | планируется |

Легковесные .NET SDK-библиотеки для интеграций с T-Bank Acquiring.

Текущий статус: последняя preview-версия — `0.2.0-preview.1`. `TBankAcquiringNet.Payments` и `TBankAcquiringNet.Multisplit.Shops` имеют реализованную SDK-поверхность. Выплаты multisplit запланированы отдельным пакетом и пока намеренно не собираются в NuGet-пакет.

## Установка

Платежи:

```bash
dotnet add package TBankAcquiringNet.Payments --prerelease
```

Регистрация магазинов multisplit:

```bash
dotnet add package TBankAcquiringNet.Multisplit.Shops --prerelease
```

Для локальной разработки можно подключить проект напрямую:

```xml
<ProjectReference Include="src/TBankAcquiringNet.Payments/TBankAcquiringNet.Payments.csproj" />
```

## Быстрый старт

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

`TerminalKey` и `Token` заполняются автоматически перед отправкой запроса. Пароль терминала используется локально для генерации SHA-256 token и не отправляется как поле запроса.

## Платежи

`TBankAcquiringNet.Payments` сейчас поддерживает:

| Метод | API клиента |
| --- | --- |
| `Init` | `InitAsync` |
| `GetState` | `GetStateAsync` |
| `CheckOrder` | `CheckOrderAsync` |
| `Cancel` | `CancelAsync` |
| `Confirm` | `ConfirmAsync` |
| `GetQr` | `GetQrAsync` |
| `ChargeQr` | `ChargeQrAsync` |

Пакет также включает:

- автоматическую генерацию token для запросов
- проверку token у платежных нотификаций
- типизированные модели ответов и статусы платежей
- описания известных кодов ошибок T-Bank
- опциональные исключения для API-ошибок в строгом режиме
- метаданные ответа с опциональным сохранением сырого тела ответа
- консервативную локальную валидацию запросов

## Нотификации

Для проверки token вебхуков используйте `TBankPaymentNotificationValidator`:

```csharp
var result = TBankPaymentNotificationValidator.Validate(notification, password);

if (!result.IsValid)
{
    return Results.BadRequest();
}

return Results.Text(TBankPaymentNotificationValidator.SuccessResponseBody);
```

## Обработка ошибок

По умолчанию API-ошибки T-Bank, например `Success=false`, возвращаются как типизированные ответы, потому что вызывающему коду часто нужны `ErrorCode`, `Message`, `Details`, `PaymentId` и `OrderId` для бизнес-решений.

Транспортные ошибки, некорректный JSON и ошибки локальной валидации выбрасываются как SDK-исключения:

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
    // Ошибка сети, DNS, таймаут или сбой HTTP-клиента на транспортном уровне.
}
catch (TBankAcquiringProtocolException ex)
{
    // Неожиданный или некорректный ответ протокола.
}
catch (TBankAcquiringValidationException ex)
{
    // Некорректный локальный запрос до отправки в T-Bank.
}
```

Установите `ThrowOnTBankApiError = true`, чтобы при ответах `Success=false` выбрасывался `TBankAcquiringApiException`.

Полная политика описана в [docs/error-handling.md](https://github.com/ai-iskuzhin/TBankAcquiringNet/blob/main/docs/error-handling.md).

## Границы пакетов

`TBankAcquiringNet.Payments` покрывает обычные acquiring-сценарии: инициализацию платежа, проверку статуса, подтверждение, отмену, QR-операции и платежные нотификации.

`TBankAcquiringNet.Multisplit.Shops` покрывает регистрацию и обновление магазинов провайдера в multisplit, включая OAuth-авторизацию регистрации, поиск по `shopCode` и банковские реквизиты для выплат.

`TBankAcquiringNet.Multisplit.Payouts` будет покрывать multisplit-выплаты, включая выплаты на карту, через СБП и партнерские выплаты.

Фреймворк-специфичные интеграционные пакеты, например `TBankAcquiringNet.AspNetCore`, стоит добавлять позже и только если SDK понадобятся DI, привязка options, webhook endpoints или health checks для ASP.NET Core.

## Документация

- [Дизайн Payments](https://github.com/ai-iskuzhin/TBankAcquiringNet/blob/main/docs/payments-design.md)
- [Обработка ошибок](https://github.com/ai-iskuzhin/TBankAcquiringNet/blob/main/docs/error-handling.md)
- [Тестирование](https://github.com/ai-iskuzhin/TBankAcquiringNet/blob/main/docs/testing.md)
- [Roadmap](https://github.com/ai-iskuzhin/TBankAcquiringNet/blob/main/docs/roadmap.md)
- [Процесс релиза](https://github.com/ai-iskuzhin/TBankAcquiringNet/blob/main/docs/release.md)

Скопированные справочные заметки по интеграциям находятся здесь:

```text
docs/integrations/t-bank-acquiring/oplata_multisplit.md
docs/integrations/t-bank-acquiring/api_reg_upd_multisplit.md
docs/integrations/t-bank-acquiring/vyplaty-multisplit.md
```

## Разработка

Запустить все тесты:

```bash
dotnet test TBankAcquiringNet.slnx
```

Собрать пакет Payments:

```bash
dotnet pack src/TBankAcquiringNet.Payments/TBankAcquiringNet.Payments.csproj --configuration Release --output artifacts/packages
```

Реальные интеграционные тесты включаются через переменные окружения:

```bash
export TBANK_ACQUIRING_TEST_TERMINAL_KEY="..."
export TBANK_ACQUIRING_TEST_PASSWORD="..."
export TBANK_ACQUIRING_TEST_BASE_URL="https://securepay.tinkoff.ru/v2/"
dotnet test tests/TBankAcquiringNet.Payments.Tests.Integration/TBankAcquiringNet.Payments.Tests.Integration.csproj
```
