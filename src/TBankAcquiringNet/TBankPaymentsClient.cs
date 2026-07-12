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

    /// <summary>
    /// Регистрирует покупателя в связке с терминалом методом AddCustomer.
    /// </summary>
    /// <remarks>API: <see href="https://developer.tbank.ru/eacq/api/add-customer">AddCustomer</see>.</remarks>
    public Task<TBankAddCustomerResponse> AddCustomerAsync(
        TBankAddCustomerRequest request,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(request);
        TBankPaymentRequestValidator.Validate(request);

        var signedRequest = request with { TerminalKey = options.TerminalKey };
        signedRequest = signedRequest with { Token = CreateToken(signedRequest, request.Token) };

        return SendAsync<TBankAddCustomerRequest, TBankAddCustomerResponse>("AddCustomer", signedRequest, cancellationToken);
    }

    /// <summary>
    /// Возвращает данные покупателя методом GetCustomer.
    /// </summary>
    /// <remarks>API: <see href="https://developer.tbank.ru/eacq/api/get-customer">GetCustomer</see>.</remarks>
    public Task<TBankGetCustomerResponse> GetCustomerAsync(
        TBankGetCustomerRequest request,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(request);
        TBankPaymentRequestValidator.Validate(request);

        var signedRequest = request with { TerminalKey = options.TerminalKey };
        signedRequest = signedRequest with { Token = CreateToken(signedRequest, request.Token) };

        return SendAsync<TBankGetCustomerRequest, TBankGetCustomerResponse>("GetCustomer", signedRequest, cancellationToken);
    }

    /// <summary>
    /// Удаляет сохраненные данные покупателя методом RemoveCustomer.
    /// </summary>
    /// <remarks>API: <see href="https://developer.tbank.ru/eacq/api/remove-customer">RemoveCustomer</see>.</remarks>
    public Task<TBankRemoveCustomerResponse> RemoveCustomerAsync(
        TBankRemoveCustomerRequest request,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(request);
        TBankPaymentRequestValidator.Validate(request);

        var signedRequest = request with { TerminalKey = options.TerminalKey };
        signedRequest = signedRequest with { Token = CreateToken(signedRequest, request.Token) };

        return SendAsync<TBankRemoveCustomerRequest, TBankRemoveCustomerResponse>("RemoveCustomer", signedRequest, cancellationToken);
    }

    /// <summary>
    /// Инициирует привязку карты к покупателю через форму привязки банка методом AddCard.
    /// </summary>
    /// <remarks>API: <see href="https://developer.tbank.ru/eacq/api/add-card">AddCard</see>. Возвращает <c>PaymentURL</c> — ссылку на форму привязки T-Bank.</remarks>
    public Task<TBankAddCardResponse> AddCardAsync(
        TBankAddCardRequest request,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(request);
        TBankPaymentRequestValidator.Validate(request);

        var signedRequest = request with { TerminalKey = options.TerminalKey };
        signedRequest = signedRequest with { Token = CreateToken(signedRequest, request.Token) };

        return SendAsync<TBankAddCardRequest, TBankAddCardResponse>("AddCard", signedRequest, cancellationToken);
    }

    /// <summary>
    /// Возвращает статус привязки карты методом GetAddCardState.
    /// </summary>
    /// <remarks>API: <see href="https://developer.tbank.ru/eacq/api/get-add-card-state">GetAddCardState</see>.</remarks>
    public Task<TBankGetAddCardStateResponse> GetAddCardStateAsync(
        TBankGetAddCardStateRequest request,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(request);
        TBankPaymentRequestValidator.Validate(request);

        var signedRequest = request with { TerminalKey = options.TerminalKey };
        signedRequest = signedRequest with { Token = CreateToken(signedRequest, request.Token) };

        return SendAsync<TBankGetAddCardStateRequest, TBankGetAddCardStateResponse>("GetAddCardState", signedRequest, cancellationToken);
    }

    /// <summary>
    /// Возвращает список привязанных карт покупателя методом GetCardList.
    /// </summary>
    /// <remarks>API: <see href="https://developer.tbank.ru/eacq/api/get-card-list">GetCardList</see>. Успешный ответ — JSON-массив карт.</remarks>
    public Task<IReadOnlyList<TBankCard>> GetCardListAsync(
        TBankGetCardListRequest request,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(request);
        TBankPaymentRequestValidator.Validate(request);

        var signedRequest = request with { TerminalKey = options.TerminalKey };
        signedRequest = signedRequest with { Token = CreateToken(signedRequest, request.Token) };

        return SendPostListAsync<TBankGetCardListRequest, TBankCard>("GetCardList", signedRequest, cancellationToken);
    }

    /// <summary>
    /// Удаляет привязанную карту покупателя методом RemoveCard.
    /// </summary>
    /// <remarks>API: <see href="https://developer.tbank.ru/eacq/api/remove-card">RemoveCard</see>.</remarks>
    public Task<TBankRemoveCardResponse> RemoveCardAsync(
        TBankRemoveCardRequest request,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(request);
        TBankPaymentRequestValidator.Validate(request);

        var signedRequest = request with { TerminalKey = options.TerminalKey };
        signedRequest = signedRequest with { Token = CreateToken(signedRequest, request.Token) };

        return SendAsync<TBankRemoveCardRequest, TBankRemoveCardResponse>("RemoveCard", signedRequest, cancellationToken);
    }

    /// <summary>
    /// Проводит платеж по сохраненным реквизитам карты (RebillId) методом Charge.
    /// </summary>
    /// <remarks>
    /// API: <see href="https://developer.tbank.ru/eacq/api/charge">Charge</see>. Сценарии:
    /// <see href="https://developer.tbank.ru/eacq/scenarios/payments/nonPCI/autopay/">без PCI DSS</see>,
    /// <see href="https://developer.tbank.ru/eacq/scenarios/payments/PCI_DSS/autopay/">с PCI DSS</see>.
    /// </remarks>
    public Task<TBankChargeResponse> ChargeAsync(
        TBankChargeRequest request,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(request);
        TBankPaymentRequestValidator.Validate(request);

        var signedRequest = request with { TerminalKey = options.TerminalKey };
        signedRequest = signedRequest with { Token = CreateToken(signedRequest, request.Token) };

        return SendAsync<TBankChargeRequest, TBankChargeResponse>("Charge", signedRequest, cancellationToken);
    }

    /// <summary>
    /// Определяет версию 3DS по карте методом Check3dsVersion (для собственной платежной формы).
    /// </summary>
    /// <remarks>
    /// API: <see href="https://developer.tbank.ru/eacq/api/check-3-ds-version">Check3dsVersion</see>.
    /// Запрос содержит зашифрованные данные карты (PCI) в <see cref="TBankCheck3dsVersionRequest.CardData"/>.
    /// </remarks>
    public Task<TBankCheck3dsVersionResponse> Check3dsVersionAsync(
        TBankCheck3dsVersionRequest request,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(request);
        TBankPaymentRequestValidator.Validate(request);

        var signedRequest = request with { TerminalKey = options.TerminalKey };
        signedRequest = signedRequest with { Token = CreateToken(signedRequest, request.Token) };

        return SendAsync<TBankCheck3dsVersionRequest, TBankCheck3dsVersionResponse>("Check3dsVersion", signedRequest, cancellationToken);
    }

    /// <summary>
    /// Завершает привязку карты на собственной платежной форме методом AttachCard.
    /// </summary>
    /// <remarks>
    /// API: <see href="https://developer.tbank.ru/eacq/api/attach-card">AttachCard</see>.
    /// Запрос содержит зашифрованные данные карты (PCI) в <see cref="TBankAttachCardRequest.CardData"/>.
    /// При статусе <c>3DS_CHECKING</c> используйте <c>ACSUrl</c>/<c>MD</c>/<c>PaReq</c> и затем Submit3DSAuthorization.
    /// </remarks>
    public Task<TBankAttachCardResponse> AttachCardAsync(
        TBankAttachCardRequest request,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(request);
        TBankPaymentRequestValidator.Validate(request);

        var signedRequest = request with { TerminalKey = options.TerminalKey };
        signedRequest = signedRequest with { Token = CreateToken(signedRequest, request.Token) };

        return SendAsync<TBankAttachCardRequest, TBankAttachCardResponse>("AttachCard", signedRequest, cancellationToken);
    }

    /// <summary>
    /// Подтверждает прохождение 3DS v1.0 методом Submit3DSAuthorization (x-www-form-urlencoded).
    /// </summary>
    /// <remarks>API: <see href="https://developer.tbank.ru/eacq/api/submit-3-ds-authorization">Submit3DSAuthorization</see>.</remarks>
    public Task<TBankSubmit3DSAuthorizationResponse> Submit3DSAuthorizationAsync(
        TBankSubmit3DSAuthorizationRequest request,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(request);
        TBankPaymentRequestValidator.Validate(request);

        var signedRequest = request with { TerminalKey = options.TerminalKey };
        signedRequest = signedRequest with { Token = CreateToken(signedRequest, request.Token) };

        var form = new List<KeyValuePair<string, string>>
        {
            new("MD", signedRequest.MD),
            new("PaRes", signedRequest.PaRes),
            new("TerminalKey", signedRequest.TerminalKey!),
        };

        if (signedRequest.PaymentId is not null)
        {
            form.Add(new("PaymentId", signedRequest.PaymentId));
        }

        if (signedRequest.Token is not null)
        {
            form.Add(new("Token", signedRequest.Token));
        }

        return SendFormAsync<TBankSubmit3DSAuthorizationResponse>("Submit3DSAuthorization", form, cancellationToken);
    }

    /// <summary>
    /// Подтверждает прохождение 3DS v2.1 методом Submit3DSAuthorizationV2 (x-www-form-urlencoded).
    /// </summary>
    /// <remarks>API: <see href="https://developer.tbank.ru/eacq/api/submit-3-ds-authorization-v-2">Submit3DSAuthorizationV2</see>.</remarks>
    public Task<TBankSubmit3DSAuthorizationResponse> Submit3DSAuthorizationV2Async(
        TBankSubmit3DSAuthorizationV2Request request,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(request);
        TBankPaymentRequestValidator.Validate(request);

        var signedRequest = request with { TerminalKey = options.TerminalKey };
        signedRequest = signedRequest with { Token = CreateToken(signedRequest, request.Token) };

        var form = new List<KeyValuePair<string, string>>
        {
            new("PaymentId", signedRequest.PaymentId),
            new("TerminalKey", signedRequest.TerminalKey!),
        };

        if (signedRequest.Token is not null)
        {
            form.Add(new("Token", signedRequest.Token));
        }

        return SendFormAsync<TBankSubmit3DSAuthorizationResponse>("Submit3DSAuthorizationV2", form, cancellationToken);
    }

    /// <summary>
    /// Генерирует справку по операциям методом getConfirmOperation.
    /// </summary>
    /// <remarks>
    /// API: <see href="https://developer.tbank.ru/eacq/api/get-confirm-operation">getConfirmOperation</see>.
    /// Token формируется только из Password и TerminalKey. Работает по картам, T-Pay и Mir Pay.
    /// При <see cref="TBankPaymentsClientOptions.ThrowOnTBankApiError"/> верхнеуровневый
    /// <c>Success = false</c> приводит к <see cref="TBankAcquiringApiException"/>; ошибки по отдельным
    /// платежам всегда остаются в <see cref="TBankGetConfirmOperationResponse.PaymentIdList"/>.
    /// </remarks>
    public async Task<TBankGetConfirmOperationResponse> GetConfirmOperationAsync(
        TBankGetConfirmOperationRequest request,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(request);
        TBankPaymentRequestValidator.Validate(request);

        // getConfirmOperation signs only TerminalKey + Password (not CallbackUrl/PaymentIdList).
        var token = options.AutoGenerateToken
            ? TBankToken.Create(new { options.TerminalKey }, options.Password)
            : request.Token;

        var signedRequest = request with { TerminalKey = options.TerminalKey, Token = token };

        var response = await SendPostRawJsonAsync<TBankGetConfirmOperationRequest, TBankGetConfirmOperationResponse>(
            "getConfirmOperation",
            signedRequest,
            static result => (result.Success, result.ErrorCode, result.Message),
            cancellationToken).ConfigureAwait(false);

        // System.Text.Json overwrites the [] initializer when the payload sends an explicit null.
        return response.PaymentIdList is null ? response with { PaymentIdList = [] } : response;
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
            var (errorCode, errorMessage, _) = TryReadErrorEnvelope(responseBody);
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

    private static (string? ErrorCode, string? Message, string? Details) TryReadErrorEnvelope(string responseBody)
    {
        try
        {
            using var document = JsonDocument.Parse(responseBody);
            if (document.RootElement.ValueKind != JsonValueKind.Object)
            {
                return (null, null, null);
            }

            var errorCode = document.RootElement.TryGetProperty("ErrorCode", out var code) ? code.GetString() : null;
            var message = document.RootElement.TryGetProperty("Message", out var msg) ? msg.GetString() : null;
            var details = document.RootElement.TryGetProperty("Details", out var det) ? det.GetString() : null;

            return (errorCode, message, details);
        }
        catch (JsonException)
        {
            return (null, null, null);
        }
    }

    private Task<TResponse> SendFormAsync<TResponse>(
        string method,
        IEnumerable<KeyValuePair<string, string>> fields,
        CancellationToken cancellationToken)
        where TResponse : TBankResponse
    {
        var httpRequest = CreateRequest(HttpMethod.Post, new Uri(options.ResolveBaseAddress(), method));
        httpRequest.Content = new FormUrlEncodedContent(fields);

        return SendJsonAsync<TResponse>(method, httpRequest, cancellationToken);
    }

    private async Task<TResponse> SendPostRawJsonAsync<TRequest, TResponse>(
        string method,
        TRequest request,
        Func<TResponse, (bool Success, string? ErrorCode, string? Message)> errorInspector,
        CancellationToken cancellationToken)
    {
        var httpRequest = CreateRequest(HttpMethod.Post, new Uri(options.ResolveBaseAddress(), method));
        httpRequest.Content = JsonContent.Create(request, options: JsonOptions);

        using var response = await SendCoreAsync(method, httpRequest, cancellationToken).ConfigureAwait(false);

#if NETSTANDARD2_0
        var responseBody = await response.Content.ReadAsStringAsync()
            .ConfigureAwait(false);
#else
        var responseBody = await response.Content.ReadAsStringAsync(cancellationToken)
            .ConfigureAwait(false);
#endif

        if (string.IsNullOrWhiteSpace(responseBody))
        {
            throw new TBankAcquiringProtocolException(
                $"T-Bank {method} response body was empty. HTTP {(int)response.StatusCode} ({response.StatusCode}).",
                response.StatusCode);
        }

        TResponse result;
        try
        {
            result = JsonSerializer.Deserialize<TResponse>(responseBody, JsonOptions)
                ?? throw new TBankAcquiringProtocolException(
                    $"T-Bank {method} response body was empty after deserialization.",
                    response.StatusCode,
                    CreateBodyPreview(responseBody));
        }
        catch (JsonException exception)
        {
            throw new TBankAcquiringProtocolException(
                $"T-Bank {method} response body was not valid JSON. HTTP {(int)response.StatusCode} ({response.StatusCode}).",
                response.StatusCode,
                CreateBodyPreview(responseBody),
                exception);
        }

        // Keep strict-mode behavior consistent with SendJsonAsync: surface a top-level failure as an
        // exception. Per-item failures inside the response are partial results and are left in place.
        var (success, errorCode, errorMessage) = errorInspector(result);
        if (options.ThrowOnTBankApiError && !success)
        {
            throw new TBankAcquiringApiException(
                $"T-Bank {method} returned ErrorCode '{errorCode}'.",
                errorCode,
                errorMessage,
                null,
                response.StatusCode);
        }

        return result;
    }

    private Task<IReadOnlyList<TItem>> SendPostListAsync<TRequest, TItem>(
        string method,
        TRequest request,
        CancellationToken cancellationToken)
    {
        var httpRequest = CreateRequest(HttpMethod.Post, new Uri(options.ResolveBaseAddress(), method));
        httpRequest.Content = JsonContent.Create(request, options: JsonOptions);

        return SendJsonListAsync<TItem>(method, httpRequest, cancellationToken);
    }

    private async Task<IReadOnlyList<TItem>> SendJsonListAsync<TItem>(
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

        // The success response is a JSON array (e.g. GetCardList). An error is a JSON object with
        // ErrorCode/Message — surface it rather than deserializing it as a list.
        if (StartsWithJsonObject(responseBody))
        {
            var (errorCode, errorMessage, details) = TryReadErrorEnvelope(responseBody);
            throw new TBankAcquiringApiException(
                $"T-Bank {method} returned ErrorCode '{errorCode}'.",
                errorCode,
                errorMessage,
                details,
                response.StatusCode);
        }

        try
        {
            return JsonSerializer.Deserialize<List<TItem>>(responseBody, JsonOptions) ?? [];
        }
        catch (JsonException exception)
        {
            throw new TBankAcquiringProtocolException(
                $"T-Bank {method} response was not a valid JSON array. HTTP {(int)response.StatusCode} ({response.StatusCode}).",
                response.StatusCode,
                CreateBodyPreview(responseBody),
                exception);
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
