# TLS-сертификаты T-API (Минцифры России)

> **ОБНОВЛЕНИЕ ОБЯЗАТЕЛЬНО.** Переход уже произошёл на боевом и тестовом контурах. Пока приложение не использует `TBankHttpClientFactory` (или пока корень Минцифры не установлен в системное хранилище), любые запросы к T-API падают на проверке TLS-сертификата.

## Что произошло

T-API перешёл с сертификатов GlobalSign на сертификаты Национального удостоверяющего центра Минцифры России (Russian Trusted CA). Проверено на живых эндпоинтах:

```text
securepay.tinkoff.ru      CN=*.tinkoff.ru  <- Russian Trusted Sub CA  <- Russian Trusted Root CA
rest-api-test.tinkoff.ru  CN=*.tinkoff.ru  <- Russian Trusted Sub CA  <- Russian Trusted Root CA
```

Оба эндпоинта отдают полную цепочку, включая корень. Корень Минцифры не входит в доверенные хранилища большинства ОС, рантаймов и контейнерных образов, поэтому необслуженный `HttpClient` перестаёт работать.

## Что встроено в пакет

| Файл | Субъект | Действителен | SHA-256 |
| --- | --- | --- | --- |
| `russian-trusted-root-ca.crt` | Russian Trusted Root CA | 2022-03-01 — 2032-02-27 | `D26D2D02…CA8ECF31` |
| `russian-trusted-sub-ca-2024.crt` | Russian Trusted Sub CA | 2024-07-15 — 2029-07-19 | `21557850…7D88A3F2` |
| `russian-trusted-sub-ca-2022.crt` | Russian Trusted Sub CA | 2022-03-02 — 2027-03-06 | `BBBDE210…D8B3FD9B` |

Сейчас T-API предъявляет промежуточный **Sub CA 2024**. Sub CA 2022 включён как запасной: промежуточные сертификаты не являются якорями доверия и лишь помогают построить цепочку.

Файлы взяты из официального дистрибутива сертификатов Минцифры ([gosuslugi.ru/crt](https://www.gosuslugi.ru/crt)) и нормализованы к LF. Отпечатки закреплены в `TBankTrustedCertificatesTests`, поэтому подмена или переупаковка сертификата ломает тесты, а не продакшен.

## Почему не встроены ГОСТ-сертификаты

В дистрибутиве Госуслуг есть ещё два сертификата — `russian_trusted_root_ca_gost_2025` и `russian_trusted_sub_ca_gost_2025` (несмотря на суффикс `_pem`, они в DER). Они бесполезны для .NET:

```text
GOST sub -> GOST root : build=False status=[PartialChain]
RSA  sub  -> RSA root : build=True  status=[]
```

.NET не умеет проверять подписи ГОСТ Р 34.10-2012 и не поддерживает ГОСТ-шифронаборы TLS ни на одной платформе. Встраивание таких якорей создало бы ложное ощущение поддержки. Если T-API когда-нибудь перейдёт на ГОСТ-only TLS, единственный путь из .NET — терминирующий прокси с поддержкой ГОСТ (stunnel + КриптоПро, `nginx` со сборкой ГОСТ и т. п.).

## Модель доверия

`TBankServerCertificateValidator` расширяет, а не подменяет системное доверие:

1. `SslPolicyErrors.None` → принять (системное хранилище уже доверяет цепочке).
2. Любая ошибка, кроме `RemoteCertificateChainErrors` — несовпадение имени хоста, отсутствие сертификата — → отклонить. Это восстановить добавлением якорей нельзя.
3. Иначе — построить цепочку заново: якоря = встроенные корни, `ExtraStore` = встроенные промежуточные + предъявленные сервером. Срок действия, подписи и ограничения проверяются как обычно.

На net8.0+ используется `X509ChainTrustMode.CustomRootTrust`. На netstandard2.0 этого API нет, поэтому цепочка строится с `AllowUnknownCertificateAuthority`, а затем корень сравнивается побайтово со встроенными якорями и отвергается любой статус, кроме `NoError`/`UntrustedRoot`. Оба пути прогоняются одним и тем же набором тестов на net10.0 через внутреннюю фабрику `CreateWithoutCustomTrustStore`.

## Альтернатива: системное хранилище

Если вы предпочитаете настроить окружение, а не приложение:

```bash
# Debian/Ubuntu
sudo cp russian_trusted_root_ca_pem.crt /usr/local/share/ca-certificates/russian_trusted_root_ca.crt
sudo update-ca-certificates

# RHEL/CentOS/Alma
sudo cp russian_trusted_root_ca_pem.crt /etc/pki/ca-trust/source/anchors/
sudo update-ca-trust
```

После этого работает любой `HttpClient`, а `TBankHttpClientFactory` остаётся безвредным (системная проверка проходит на шаге 1).

## Ротация

Встроенные сертификаты придётся обновить к **2027-03-06** (Sub CA 2022), **2029-07-19** (Sub CA 2024) и **2032-02-27** (корень). Тест `BundledCertificates_AreCertificateAuthoritiesThatAreStillValid` начнёт падать по мере приближения срока.

Живая проверка соответствия встроенных сертификатов тому, что реально отдаёт T-API:

```bash
TBANK_ACQUIRING_LIVE_TLS=1 dotnet test tests/TBankAcquiringNet.Tests.Integration
```
