using System.Net.Http.Json;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Text.Json;
using System.Text.RegularExpressions;
using System.Text.Json.Serialization;

namespace TBankAcquiringNet;

/// <summary>
/// Клиент платежных методов T-Bank acquiring.
/// </summary>
public sealed class TBankPaymentsClient
{
    private static readonly JsonSerializerOptions JsonOptions = CreateJsonOptions();
    private static readonly string UserAgent = BuildUserAgent();

    private readonly HttpClient httpClient;
    private readonly TBankPaymentsClientOptions options;

    /// <summary>
    /// Создает клиент платежного API T-Bank.
    /// </summary>
    public TBankPaymentsClient(HttpClient httpClient, TBankPaymentsClientOptions options)
    {
        ArgumentNullException.ThrowIfNull(httpClient);
        ArgumentNullException.ThrowIfNull(options);

        if (string.IsNullOrWhiteSpace(options.TerminalKey))
        {
            throw new ArgumentException("Terminal key must be configured.", nameof(options));
        }

        if (string.IsNullOrWhiteSpace(options.Password))
        {
            throw new ArgumentException("Password must be configured.", nameof(options));
        }

        this.httpClient = httpClient;
        this.options = options;
    }

    /// <summary>
    /// Инициирует платежную сессию методом Init.
    /// </summary>
    /// <remarks>
    /// Точка входа платежного сценария (Init → FinishAuthorize → Confirm). Сценарии оплаты:
    /// <see href="https://developer.tbank.ru/eacq/scenarios/payments/nonPCI/">без PCI DSS (редирект на платежную форму T‑Bank)</see>
    /// и <see href="https://developer.tbank.ru/eacq/scenarios/payments/PCI_DSS/">с PCI DSS (своя платежная форма)</see>.
    /// Для кошельков T‑Pay и SberPay Web параметры устройства передаются через
    /// <see cref="TBankInitPaymentRequest.DATA"/> — см. <see cref="TBankInitDataKeys"/>.
    /// </remarks>
    public Task<TBankInitPaymentResponse> InitAsync(
        TBankInitPaymentRequest request,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(request);
        TBankPaymentRequestValidator.Validate(request);

        var signedRequest = request with
        {
            TerminalKey = options.TerminalKey
        };

        signedRequest = signedRequest with
        {
            Token = CreateToken(signedRequest, request.Token)
        };

        return SendAsync<TBankInitPaymentRequest, TBankInitPaymentResponse>("Init", signedRequest, cancellationToken);
    }

    /// <summary>
    /// Возвращает состояние платежа методом GetState.
    /// </summary>
    public Task<TBankPaymentStateResponse> GetStateAsync(
        TBankPaymentStateRequest request,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(request);
        TBankPaymentRequestValidator.Validate(request);

        var signedRequest = request with
        {
            TerminalKey = options.TerminalKey
        };

        signedRequest = signedRequest with
        {
            Token = CreateToken(signedRequest, request.Token)
        };

        return SendAsync<TBankPaymentStateRequest, TBankPaymentStateResponse>("GetState", signedRequest, cancellationToken);
    }

    /// <summary>
    /// Возвращает состояние заказа и связанные платежи методом CheckOrder.
    /// </summary>
    public Task<TBankCheckOrderResponse> CheckOrderAsync(
        TBankCheckOrderRequest request,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(request);
        TBankPaymentRequestValidator.Validate(request);

        var signedRequest = request with
        {
            TerminalKey = options.TerminalKey
        };

        signedRequest = signedRequest with
        {
            Token = CreateToken(signedRequest, request.Token)
        };

        return SendAsync<TBankCheckOrderRequest, TBankCheckOrderResponse>("CheckOrder", signedRequest, cancellationToken);
    }

    /// <summary>
    /// Отменяет платеж или выполняет возврат методом Cancel.
    /// </summary>
    /// <remarks>
    /// Для SberPay и Alfa Pay отмена (до списания) выполняется один раз и на полную сумму;
    /// возврат (после списания) — несколько раз, частично или полностью, пока не исчерпана сумма
    /// платежа. При нагрузке на стороне провайдера статус может вернуться <c>REVERSING</c>/<c>REFUNDING</c>
    /// и перейти в <c>REVERSED</c>/<c>REFUNDED</c> в течение ~2 минут — опрашивайте GetState.
    /// </remarks>
    public Task<TBankCancelPaymentResponse> CancelAsync(
        TBankCancelPaymentRequest request,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(request);
        TBankPaymentRequestValidator.Validate(request);

        var signedRequest = request with
        {
            TerminalKey = options.TerminalKey
        };

        signedRequest = signedRequest with
        {
            Token = CreateToken(signedRequest, request.Token)
        };

        return SendAsync<TBankCancelPaymentRequest, TBankCancelPaymentResponse>("Cancel", signedRequest, cancellationToken);
    }

    /// <summary>
    /// Подтверждает авторизованный платеж методом Confirm.
    /// </summary>
    /// <remarks>
    /// Завершает двухстадийную оплату (Init → FinishAuthorize → Confirm). Сценарии:
    /// <see href="https://developer.tbank.ru/eacq/scenarios/payments/nonPCI/">без PCI DSS</see>,
    /// <see href="https://developer.tbank.ru/eacq/scenarios/payments/PCI_DSS/">с PCI DSS</see>.
    /// Для SberPay и Alfa Pay подтвердить сессию можно только один раз (частично или полностью).
    /// При нагрузке на стороне провайдера статус может вернуться <c>CONFIRMING</c> и перейти в
    /// <c>CONFIRMED</c> в течение ~2 минут — опрашивайте состояние методом GetState.
    /// </remarks>
    public Task<TBankConfirmPaymentResponse> ConfirmAsync(
        TBankConfirmPaymentRequest request,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(request);
        TBankPaymentRequestValidator.Validate(request);

        var signedRequest = request with
        {
            TerminalKey = options.TerminalKey
        };

        signedRequest = signedRequest with
        {
            Token = CreateToken(signedRequest, request.Token)
        };

        return SendAsync<TBankConfirmPaymentRequest, TBankConfirmPaymentResponse>("Confirm", signedRequest, cancellationToken);
    }

    /// <summary>
    /// Регистрирует QR и возвращает payload или изображение методом GetQr.
    /// </summary>
    /// <remarks>Сценарий: <see href="https://developer.tbank.ru/eacq/scenarios/payments/PCI_DSS/sbp/">приём платежей через СБП на своей платежной форме</see>.</remarks>
    public Task<TBankQrResponse> GetQrAsync(
        TBankQrRequest request,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(request);
        TBankPaymentRequestValidator.Validate(request);

        var signedRequest = request with
        {
            TerminalKey = options.TerminalKey
        };

        signedRequest = signedRequest with
        {
            Token = CreateToken(signedRequest, request.Token)
        };

        return SendAsync<TBankQrRequest, TBankQrResponse>("GetQr", signedRequest, cancellationToken);
    }

    /// <summary>
    /// Выполняет списание по привязанному QR-счету методом ChargeQr.
    /// </summary>
    public Task<TBankChargeQrResponse> ChargeQrAsync(
        TBankChargeQrRequest request,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(request);
        TBankPaymentRequestValidator.Validate(request);

        var signedRequest = request with
        {
            TerminalKey = options.TerminalKey
        };

        signedRequest = signedRequest with
        {
            Token = CreateToken(signedRequest, request.Token)
        };

        return SendAsync<TBankChargeQrRequest, TBankChargeQrResponse>("ChargeQr", signedRequest, cancellationToken);
    }

    /// <summary>
    /// Возвращает статус возврата платежа по СБП методом GetQrState.
    /// </summary>
    /// <remarks>
    /// API: <see href="https://developer.tbank.ru/eacq/api/get-qr-state">GetQrState</see>.
    /// Способы получения данных о платеже: <see href="https://developer.tbank.ru/eacq/scenarios/notification">нотификации</see>.
    /// </remarks>
    public Task<TBankQrStateResponse> GetQrStateAsync(
        TBankQrStateRequest request,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(request);
        TBankPaymentRequestValidator.Validate(request);

        var signedRequest = request with
        {
            TerminalKey = options.TerminalKey
        };

        signedRequest = signedRequest with
        {
            Token = CreateToken(signedRequest, request.Token)
        };

        return SendAsync<TBankQrStateRequest, TBankQrStateResponse>("GetQrState", signedRequest, cancellationToken);
    }

    /// <summary>
    /// Возвращает список банков-участников СБП методом GetQrBankList.
    /// </summary>
    /// <remarks>API: <see href="https://developer.tbank.ru/eacq/api/get-qr-bank-list">GetQrBankList</see>.</remarks>
    public Task<TBankQrBankListResponse> GetQrBankListAsync(
        TBankQrBankListRequest request,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(request);
        TBankPaymentRequestValidator.Validate(request);

        var signedRequest = request with
        {
            TerminalKey = options.TerminalKey
        };

        signedRequest = signedRequest with
        {
            Token = CreateToken(signedRequest, request.Token)
        };

        return SendAsync<TBankQrBankListRequest, TBankQrBankListResponse>("GetQrBankList", signedRequest, cancellationToken);
    }

    /// <summary>
    /// Возвращает список привязанных к магазину счетов методом GetAccountQrList.
    /// </summary>
    /// <remarks>API: <see href="https://developer.tbank.ru/eacq/api/get-account-qr-list">GetAccountQrList</see>.</remarks>
    public Task<TBankAccountQrListResponse> GetAccountQrListAsync(
        TBankAccountQrListRequest request,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(request);
        TBankPaymentRequestValidator.Validate(request);

        var signedRequest = request with
        {
            TerminalKey = options.TerminalKey
        };

        signedRequest = signedRequest with
        {
            Token = CreateToken(signedRequest, request.Token)
        };

        return SendAsync<TBankAccountQrListRequest, TBankAccountQrListResponse>("GetAccountQrList", signedRequest, cancellationToken);
    }

    /// <summary>
    /// Возвращает статус привязки счета к магазину методом GetAddAccountQrState.
    /// </summary>
    /// <remarks>API: <see href="https://developer.tbank.ru/eacq/api/get-add-account-qr-state">GetAddAccountQrState</see>.</remarks>
    public Task<TBankAddAccountQrStateResponse> GetAddAccountQrStateAsync(
        TBankAddAccountQrStateRequest request,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(request);
        TBankPaymentRequestValidator.Validate(request);

        var signedRequest = request with
        {
            TerminalKey = options.TerminalKey
        };

        signedRequest = signedRequest with
        {
            Token = CreateToken(signedRequest, request.Token)
        };

        return SendAsync<TBankAddAccountQrStateRequest, TBankAddAccountQrStateResponse>("GetAddAccountQrState", signedRequest, cancellationToken);
    }

    /// <summary>
    /// Привязывает счет покупателя к магазину методом AddAccountQr.
    /// </summary>
    /// <remarks>
    /// API: <see href="https://developer.tbank.ru/eacq/api/add-account-qr">AddAccountQr</see>.
    /// Сценарии: <see href="https://developer.tbank.ru/eacq/scenarios/payments/nonPCI/autopay/">платежи по сохраненным реквизитам через СБП</see>,
    /// <see href="https://developer.tbank.ru/eacq/scenarios/payments/PCI_DSS/autopay/">по сохраненным реквизитам на своей платежной форме</see>,
    /// <see href="https://developer.tbank.ru/eacq/scenarios/payments/PCI_DSS/sbp/">платеж через СБП на своей платежной форме</see>.
    /// </remarks>
    public Task<TBankAddAccountQrResponse> AddAccountQrAsync(
        TBankAddAccountQrRequest request,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(request);
        TBankPaymentRequestValidator.Validate(request);

        var signedRequest = request with
        {
            TerminalKey = options.TerminalKey
        };

        signedRequest = signedRequest with
        {
            Token = CreateToken(signedRequest, request.Token)
        };

        return SendAsync<TBankAddAccountQrRequest, TBankAddAccountQrResponse>("AddAccountQr", signedRequest, cancellationToken);
    }

    /// <summary>
    /// Возвращает список банков-участников QR для возврата методом QrMembersList.
    /// </summary>
    /// <remarks>API: <see href="https://developer.tbank.ru/eacq/api/qr-members-list">QrMembersList</see>.</remarks>
    public Task<TBankQrMembersListResponse> QrMembersListAsync(
        TBankQrMembersListRequest request,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(request);
        TBankPaymentRequestValidator.Validate(request);

        var signedRequest = request with
        {
            TerminalKey = options.TerminalKey
        };

        signedRequest = signedRequest with
        {
            Token = CreateToken(signedRequest, request.Token)
        };

        return SendAsync<TBankQrMembersListRequest, TBankQrMembersListResponse>("QrMembersList", signedRequest, cancellationToken);
    }

    /// <summary>
    /// Создает тестовую платежную сессию СБП методом SbpPayTest. Доступно только на тестовом терминале.
    /// </summary>
    /// <remarks>
    /// API: <see href="https://developer.tbank.ru/eacq/api/sbp-pay-test">SbpPayTest</see>.
    /// Сценарий тестирования: <see href="https://developer.tbank.ru/eacq/intro/errors/test-sbp">тестовая оплата по СБП</see>.
    /// </remarks>
    public Task<TBankSbpPayTestResponse> SbpPayTestAsync(
        TBankSbpPayTestRequest request,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(request);
        TBankPaymentRequestValidator.Validate(request);

        var signedRequest = request with
        {
            TerminalKey = options.TerminalKey
        };

        signedRequest = signedRequest with
        {
            Token = CreateToken(signedRequest, request.Token)
        };

        return SendAsync<TBankSbpPayTestRequest, TBankSbpPayTestResponse>("SbpPayTest", signedRequest, cancellationToken);
    }

    /// <summary>
    /// Проверяет доступность T‑Pay на терминале методом TinkoffPay status.
    /// </summary>
    /// <remarks>
    /// API: <see href="https://developer.tbank.ru/eacq/api/status">status</see>.
    /// Сценарий: <see href="https://developer.tbank.ru/eacq/scenarios/payments/PCI_DSS/t-pay/">приём платежей через T‑Pay</see>.
    /// </remarks>
    public Task<TBankTinkoffPayStatusResponse> GetTinkoffPayStatusAsync(
        CancellationToken cancellationToken = default)
    {
        var path = $"TinkoffPay/terminals/{Uri.EscapeDataString(options.TerminalKey)}/status";

        return SendGetJsonAsync<TBankTinkoffPayStatusResponse>("TinkoffPay status", path, cancellationToken);
    }

    /// <summary>
    /// Возвращает ссылку T‑Pay для безусловного редиректа покупателя методом TinkoffPay link.
    /// </summary>
    /// <remarks>Сценарий: <see href="https://developer.tbank.ru/eacq/scenarios/payments/PCI_DSS/t-pay/">приём платежей через T‑Pay</see>.</remarks>
    public Task<TBankTinkoffPayLinkResponse> GetTinkoffPayLinkAsync(
        string paymentId,
        string version,
        CancellationToken cancellationToken = default)
    {
        RequireArgument(paymentId, nameof(paymentId));
        RequireArgument(version, nameof(version));

        var path = $"TinkoffPay/transactions/{Uri.EscapeDataString(paymentId)}/versions/{Uri.EscapeDataString(version)}/link";

        return SendGetJsonAsync<TBankTinkoffPayLinkResponse>("TinkoffPay link", path, cancellationToken);
    }

    /// <summary>
    /// Возвращает SVG QR-кода T‑Pay для десктопов методом TinkoffPay QR.
    /// </summary>
    /// <remarks>Сценарий: <see href="https://developer.tbank.ru/eacq/scenarios/payments/PCI_DSS/t-pay/">приём платежей через T‑Pay</see>.</remarks>
    public Task<string> GetTinkoffPayQrAsync(
        string paymentId,
        CancellationToken cancellationToken = default)
    {
        RequireArgument(paymentId, nameof(paymentId));

        var path = $"TinkoffPay/{Uri.EscapeDataString(paymentId)}/QR";

        return SendGetSvgAsync("TinkoffPay QR", path, cancellationToken);
    }

    /// <summary>
    /// Возвращает SVG QR-кода SberPay для десктопов методом SberPay QR.
    /// </summary>
    /// <remarks>Сценарий: <see href="https://developer.tbank.ru/eacq/scenarios/payments/PCI_DSS/sberpay/">приём платежей через SberPay</see>.</remarks>
    public Task<string> GetSberPayQrAsync(
        string paymentId,
        CancellationToken cancellationToken = default)
    {
        RequireArgument(paymentId, nameof(paymentId));

        var path = $"SberPay/{Uri.EscapeDataString(paymentId)}/QR";

        return SendGetSvgAsync("SberPay QR", path, cancellationToken);
    }

    /// <summary>
    /// Возвращает ссылку SberPay для редиректа покупателя методом SberPay link.
    /// </summary>
    /// <remarks>Сценарий: <see href="https://developer.tbank.ru/eacq/scenarios/payments/PCI_DSS/sberpay/">приём платежей через SberPay</see>.</remarks>
    public Task<TBankSberPayLinkResponse> GetSberPayLinkAsync(
        string paymentId,
        CancellationToken cancellationToken = default)
    {
        RequireArgument(paymentId, nameof(paymentId));

        var path = $"SberPay/transactions/{Uri.EscapeDataString(paymentId)}/link";

        return SendGetJsonAsync<TBankSberPayLinkResponse>("SberPay link", path, cancellationToken);
    }

    /// <summary>
    /// Возвращает подписанный DeepLink Mir Pay методом MirPay/GetDeepLink.
    /// </summary>
    /// <remarks>
    /// DeepLink открывается только на мобильных устройствах Android — приложение Mir Pay доступно исключительно на Android.
    /// Сценарий: <see href="https://developer.tbank.ru/eacq/scenarios/payments/PCI_DSS/mirpay/">оплата через Mir Pay на своей платежной форме</see>.
    /// </remarks>
    public Task<TBankMirPayDeepLinkResponse> GetMirPayDeepLinkAsync(
        TBankMirPayDeepLinkRequest request,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(request);
        TBankPaymentRequestValidator.Validate(request);

        var signedRequest = request with
        {
            TerminalKey = options.TerminalKey
        };

        signedRequest = signedRequest with
        {
            Token = CreateToken(signedRequest, request.Token)
        };

        return SendAsync<TBankMirPayDeepLinkRequest, TBankMirPayDeepLinkResponse>("MirPay/GetDeepLink", signedRequest, cancellationToken);
    }

    /// <summary>
    /// Возвращает ссылку Alfa Pay методом AlfaPay/link/get.
    /// </summary>
    /// <remarks>Сценарий: <see href="https://developer.tbank.ru/eacq/scenarios/payments/PCI_DSS/alfapay">приём платежей через Alfa Pay на своей платежной форме</see>.</remarks>
    public Task<TBankAlfaPayLinkResponse> GetAlfaPayLinkAsync(
        TBankAlfaPayLinkRequest request,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(request);
        TBankPaymentRequestValidator.Validate(request);

        var signedRequest = request with
        {
            TerminalKey = options.TerminalKey
        };

        signedRequest = signedRequest with
        {
            Token = CreateToken(signedRequest, request.Token)
        };

        return SendAsync<TBankAlfaPayLinkRequest, TBankAlfaPayLinkResponse>("AlfaPay/link/get", signedRequest, cancellationToken);
    }

    /// <summary>
    /// Отправляет закрывающий чек в кассу методом SendClosingReceipt (ФФД 1.2).
    /// </summary>
    /// <remarks>Сценарий: <see href="https://developer.tbank.ru/eacq/scenarios/fiscalization/">работа с чеками (фискализация)</see>.</remarks>
    public Task<TBankSendClosingReceiptResponse> SendClosingReceiptAsync(
        TBankSendClosingReceiptFfd12Request request,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(request);
        TBankPaymentRequestValidator.Validate(request);

        var signedRequest = request with
        {
            TerminalKey = options.TerminalKey
        };

        signedRequest = signedRequest with
        {
            Token = CreateToken(signedRequest, request.Token)
        };

        return SendPostAsync<TBankSendClosingReceiptFfd12Request, TBankSendClosingReceiptResponse>("SendClosingReceipt", CashboxEndpoint("SendClosingReceipt"), signedRequest, cancellationToken);
    }

    /// <summary>
    /// Отправляет закрывающий чек в кассу методом SendClosingReceipt (ФФД 1.05).
    /// </summary>
    /// <remarks>Сценарий: <see href="https://developer.tbank.ru/eacq/scenarios/fiscalization/">работа с чеками (фискализация)</see>.</remarks>
    public Task<TBankSendClosingReceiptResponse> SendClosingReceiptAsync(
        TBankSendClosingReceiptFfd105Request request,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(request);
        TBankPaymentRequestValidator.Validate(request);

        var signedRequest = request with
        {
            TerminalKey = options.TerminalKey
        };

        signedRequest = signedRequest with
        {
            Token = CreateToken(signedRequest, request.Token)
        };

        return SendPostAsync<TBankSendClosingReceiptFfd105Request, TBankSendClosingReceiptResponse>("SendClosingReceipt", CashboxEndpoint("SendClosingReceipt"), signedRequest, cancellationToken);
    }

    // Cashbox endpoints live at the host root (/cashbox/...), not under the /v2/ base path.
    private Uri CashboxEndpoint(string method) => new(options.ResolveBaseAddress(), $"/cashbox/{method}");

    private static void RequireArgument(string value, string parameterName)
    {
        ArgumentNullException.ThrowIfNull(value, parameterName);

        if (string.IsNullOrWhiteSpace(value))
        {
            throw new ArgumentException($"{parameterName} must not be empty.", parameterName);
        }
    }

    private string? CreateToken<TRequest>(TRequest request, string? suppliedToken)
    {
        if (!options.AutoGenerateToken)
        {
            return suppliedToken;
        }

        return TBankToken.Create(request, options.Password);
    }

    private Task<TResponse> SendAsync<TRequest, TResponse>(
        string method,
        TRequest request,
        CancellationToken cancellationToken)
        where TResponse : TBankResponse
    {
        return SendPostAsync<TRequest, TResponse>(method, new Uri(options.ResolveBaseAddress(), method), request, cancellationToken);
    }

    private Task<TResponse> SendPostAsync<TRequest, TResponse>(
        string method,
        Uri endpoint,
        TRequest request,
        CancellationToken cancellationToken)
        where TResponse : TBankResponse
    {
        var httpRequest = CreateRequest(HttpMethod.Post, endpoint);
        httpRequest.Content = JsonContent.Create(request, options: JsonOptions);

        return SendJsonAsync<TResponse>(method, httpRequest, cancellationToken);
    }

    private Task<TResponse> SendGetJsonAsync<TResponse>(
        string method,
        string relativePath,
        CancellationToken cancellationToken)
        where TResponse : TBankResponse
    {
        var endpoint = new Uri(options.ResolveBaseAddress(), relativePath);

        return SendJsonAsync<TResponse>(method, CreateRequest(HttpMethod.Get, endpoint), cancellationToken);
    }

    private Task<string> SendGetSvgAsync(
        string method,
        string relativePath,
        CancellationToken cancellationToken)
    {
        var endpoint = new Uri(options.ResolveBaseAddress(), relativePath);
        var httpRequest = CreateRequest(HttpMethod.Get, endpoint);
        // T-Bank rejects the non-standard "image/svg" media type with HTTP 415; the registered
        // IANA type is "image/svg+xml".
        httpRequest.Headers.Accept.ParseAdd("image/svg+xml");

        return SendSvgAsync(method, httpRequest, cancellationToken);
    }

    private HttpRequestMessage CreateRequest(HttpMethod httpMethod, Uri endpoint)
    {
        var httpRequest = new HttpRequestMessage(httpMethod, endpoint);
        httpRequest.Headers.UserAgent.ParseAdd(UserAgent);

        return httpRequest;
    }

    private async Task<HttpResponseMessage> SendCoreAsync(
        string method,
        HttpRequestMessage httpRequest,
        CancellationToken cancellationToken)
    {
        using (httpRequest)
        {
            try
            {
                return await httpClient.SendAsync(httpRequest, cancellationToken)
                    .ConfigureAwait(false);
            }
            catch (HttpRequestException exception)
            {
                throw new TBankAcquiringTransportException($"T-Bank {method} request failed before a response was received.", exception);
            }
        }
    }

    private async Task<TResponse> SendJsonAsync<TResponse>(
        string method,
        HttpRequestMessage httpRequest,
        CancellationToken cancellationToken)
        where TResponse : TBankResponse
    {
        using var response = await SendCoreAsync(method, httpRequest, cancellationToken).ConfigureAwait(false);

#if NETSTANDARD2_0
        var responseBody = await response.Content.ReadAsStringAsync()
            .ConfigureAwait(false);
#else
        var responseBody = await response.Content.ReadAsStringAsync(cancellationToken)
            .ConfigureAwait(false);
#endif

        var result = DeserializeResponse<TResponse>(method, response.StatusCode, responseBody);
        result = result with
        {
            Metadata = CreateResponseMetadata(response, responseBody, options.CaptureRawResponseBody)
        };

        if (options.ThrowOnTBankApiError && !result.Success)
        {
            throw new TBankAcquiringApiException(
                $"T-Bank {method} returned ErrorCode '{result.ErrorCode}'.",
                result.ErrorCode,
                result.Message,
                result.Details,
                response.StatusCode);
        }

        return result;
    }

    private async Task<string> SendSvgAsync(
        string method,
        HttpRequestMessage httpRequest,
        CancellationToken cancellationToken)
    {
        using var response = await SendCoreAsync(method, httpRequest, cancellationToken).ConfigureAwait(false);

#if NETSTANDARD2_0
        var responseBody = await response.Content.ReadAsStringAsync()
            .ConfigureAwait(false);
#else
        var responseBody = await response.Content.ReadAsStringAsync(cancellationToken)
            .ConfigureAwait(false);
#endif

        if (!response.IsSuccessStatusCode)
        {
            throw new TBankAcquiringProtocolException(
                $"T-Bank {method} returned HTTP {(int)response.StatusCode} ({response.StatusCode}).",
                response.StatusCode,
                CreateBodyPreview(responseBody));
        }

        if (string.IsNullOrWhiteSpace(responseBody))
        {
            throw new TBankAcquiringProtocolException(
                $"T-Bank {method} returned an empty SVG body. HTTP {(int)response.StatusCode} ({response.StatusCode}).",
                response.StatusCode);
        }

        // T-Bank can answer HTTP 200 with a JSON error envelope instead of an SVG image
        // (e.g. {"Success":false,"ErrorCode":"8","Message":"Неверный статус транзакции."}).
        // An SVG document starts with '<'; a JSON object starts with '{'. Surface the error
        // rather than returning JSON as if it were an image.
        if (StartsWithJsonObject(responseBody))
        {
            var (errorCode, errorMessage) = TryReadErrorEnvelope(responseBody);
            throw new TBankAcquiringProtocolException(
                $"T-Bank {method} returned a JSON body instead of an SVG image. ErrorCode '{errorCode}', Message '{errorMessage}'.",
                response.StatusCode,
                CreateBodyPreview(responseBody));
        }

        return responseBody;
    }

    private static bool StartsWithJsonObject(string body)
    {
        foreach (var character in body)
        {
            if (char.IsWhiteSpace(character))
            {
                continue;
            }

            return character == '{';
        }

        return false;
    }

    private static (string? ErrorCode, string? Message) TryReadErrorEnvelope(string responseBody)
    {
        try
        {
            using var document = JsonDocument.Parse(responseBody);
            if (document.RootElement.ValueKind != JsonValueKind.Object)
            {
                return (null, null);
            }

            var errorCode = document.RootElement.TryGetProperty("ErrorCode", out var code) ? code.GetString() : null;
            var message = document.RootElement.TryGetProperty("Message", out var msg) ? msg.GetString() : null;

            return (errorCode, message);
        }
        catch (JsonException)
        {
            return (null, null);
        }
    }

    private static TBankAcquiringResponseMetadata CreateResponseMetadata(
        HttpResponseMessage response,
        string responseBody,
        bool captureRawResponseBody)
    {
        var headers = response.Headers
            .Concat(response.Content.Headers)
            .GroupBy(static header => header.Key, StringComparer.OrdinalIgnoreCase)
            .ToDictionary(
                static group => group.Key,
                static group => group.SelectMany(static header => header.Value).ToArray(),
                StringComparer.OrdinalIgnoreCase);

        return new TBankAcquiringResponseMetadata(
            response.StatusCode,
            headers,
            captureRawResponseBody ? responseBody : null);
    }

    private static TResponse DeserializeResponse<TResponse>(
        string method,
        System.Net.HttpStatusCode statusCode,
        string responseBody)
        where TResponse : TBankResponse
    {
        if (string.IsNullOrWhiteSpace(responseBody))
        {
            throw new TBankAcquiringProtocolException(
                $"T-Bank {method} response body was empty. HTTP {(int)statusCode} ({statusCode}).",
                statusCode);
        }

        try
        {
            return JsonSerializer.Deserialize<TResponse>(responseBody, JsonOptions)
                ?? throw new TBankAcquiringProtocolException(
                    $"T-Bank {method} response body was empty after deserialization.",
                    statusCode,
                    CreateBodyPreview(responseBody));
        }
        catch (JsonException exception)
        {
            var responseBodyPreview = CreateBodyPreview(responseBody);
            throw new TBankAcquiringProtocolException(
                $"T-Bank {method} response body was not valid JSON for the expected response model. HTTP {(int)statusCode} ({statusCode}). Response preview: {responseBodyPreview}",
                statusCode,
                responseBodyPreview,
                exception);
        }
    }

    private static string CreateBodyPreview(string responseBody)
    {
        var preview = RedactSensitiveFields(responseBody);
        const int maxLength = 512;

        return preview.Length <= maxLength ? preview : preview[..maxLength];
    }

    private static string RedactSensitiveFields(string value)
    {
        return Regex.Replace(
            value,
            "(\"(?:Token|Password|CardData|CVV|DigestValue|SignatureValue)\"\\s*:\\s*\")([^\"]*)(\")",
            "$1***REDACTED***$3",
            RegexOptions.IgnoreCase | RegexOptions.CultureInvariant);
    }

    private static string BuildUserAgent()
    {
        var assembly = typeof(TBankPaymentsClient).Assembly;
        var version = assembly.GetCustomAttribute<AssemblyInformationalVersionAttribute>()?.InformationalVersion
            ?? assembly.GetName().Version?.ToString()
            ?? "0.0.0";

        // Strip Source Link build metadata (e.g. "1.0.0+abc123") from the product version.
        var metadataSeparator = version.IndexOf('+');
        if (metadataSeparator >= 0)
        {
            version = version.Substring(0, metadataSeparator);
        }

        return $"TBankAcquiringNet/{version} ({RuntimeInformation.FrameworkDescription})";
    }

    private static JsonSerializerOptions CreateJsonOptions()
    {
        var jsonOptions = new JsonSerializerOptions(JsonSerializerDefaults.Web)
        {
            DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull,
            PropertyNamingPolicy = null
        };

        jsonOptions.Converters.Add(new JsonStringEnumConverter());
        jsonOptions.Converters.Add(new TBankAmountJsonConverter());
        jsonOptions.Converters.Add(new TBankPaymentStatusJsonConverter());
        jsonOptions.Converters.Add(new TBankQrDataTypeJsonConverter());

        return jsonOptions;
    }
}
