using System.Text.Json.Serialization;

namespace TBankAcquiringNet;

/// <summary>
/// Запрос Init для создания платежной сессии.
/// </summary>
public sealed record TBankInitPaymentRequest
{
    /// <summary>Ключ терминала. Обычно заполняется клиентом автоматически.</summary>
    public string? TerminalKey { get; init; }

    /// <summary>Сумма платежа в минимальных единицах валюты.</summary>
    [JsonConverter(typeof(TBankAmountJsonConverter))]
    public TBankAmount Amount { get; init; }

    /// <summary>Номер заказа на стороне площадки.</summary>
    public required string OrderId { get; init; }

    /// <summary>Подпись запроса. Обычно генерируется клиентом автоматически.</summary>
    public string? Token { get; init; }

    /// <summary>IP-адрес клиента.</summary>
    public string? IP { get; init; }

    /// <summary>Описание платежа.</summary>
    public string? Description { get; init; }

    /// <summary>Идентификатор получателя выплаты в multisplit-сценарии.</summary>
    public string? PaymentRecipientId { get; init; }

    /// <summary>Идентификатор сделки.</summary>
    public string? DealId { get; init; }

    /// <summary>Флаг создания сделки, если DealId не передан.</summary>
    public string? CreateDealWithType { get; init; }

    /// <summary>Уровень проверки получателя выплаты.</summary>
    public string? LevelOfConfidence { get; init; }

    /// <summary>Код валюты ISO 4217.</summary>
    public int? Currency { get; init; }

    /// <summary>Идентификатор покупателя.</summary>
    public string? CustomerKey { get; init; }

    /// <summary>Признак регистрации автоплатежа.</summary>
    [JsonConverter(typeof(TBankRecurrentJsonConverter))]
    public TBankRecurrent? Recurrent { get; init; }

    /// <summary>Тип оплаты. Если не задан, используется настройка терминала.</summary>
    [JsonConverter(typeof(TBankPayTypeJsonConverter))]
    public TBankPayType? PayType { get; init; }

    /// <summary>Язык платежной формы. По умолчанию ru.</summary>
    [JsonConverter(typeof(TBankLanguageJsonConverter))]
    public TBankLanguage? Language { get; init; }

    /// <summary>URL для HTTP-нотификаций.</summary>
    public Uri? NotificationURL { get; init; }

    /// <summary>URL редиректа при успешной оплате.</summary>
    public Uri? SuccessURL { get; init; }

    /// <summary>URL редиректа при неуспешной оплате.</summary>
    public Uri? FailURL { get; init; }

    /// <summary>Срок жизни платежной ссылки или QR.</summary>
    public DateTimeOffset? RedirectDueDate { get; init; }

    /// <summary>Дополнительные параметры платежа.</summary>
    public IReadOnlyDictionary<string, string?>? DATA { get; init; }
}

/// <summary>
/// Запрос GetState для получения статуса платежа.
/// </summary>
public sealed record TBankPaymentStateRequest
{
    /// <summary>Ключ терминала. Обычно заполняется клиентом автоматически.</summary>
    public string? TerminalKey { get; init; }

    /// <summary>Идентификатор платежа в T-Bank.</summary>
    public required string PaymentId { get; init; }

    /// <summary>Подпись запроса. Обычно генерируется клиентом автоматически.</summary>
    public string? Token { get; init; }

    /// <summary>IP-адрес клиента.</summary>
    public string? IP { get; init; }
}

/// <summary>
/// Запрос CheckOrder для получения платежей по заказу.
/// </summary>
public sealed record TBankCheckOrderRequest
{
    /// <summary>Ключ терминала. Обычно заполняется клиентом автоматически.</summary>
    public string? TerminalKey { get; init; }

    /// <summary>Номер заказа на стороне площадки.</summary>
    public required string OrderId { get; init; }

    /// <summary>Подпись запроса. Обычно генерируется клиентом автоматически.</summary>
    public string? Token { get; init; }
}

/// <summary>
/// Запрос Cancel для отмены платежа или возврата.
/// </summary>
public sealed record TBankCancelPaymentRequest
{
    /// <summary>Ключ терминала. Обычно заполняется клиентом автоматически.</summary>
    public string? TerminalKey { get; init; }

    /// <summary>Идентификатор платежа в T-Bank.</summary>
    public required string PaymentId { get; init; }

    /// <summary>Подпись запроса. Обычно генерируется клиентом автоматически.</summary>
    public string? Token { get; init; }

    /// <summary>IP-адрес клиента.</summary>
    public string? IP { get; init; }

    /// <summary>Сумма отмены или возврата.</summary>
    [JsonConverter(typeof(TBankAmountJsonConverter))]
    public TBankAmount? Amount { get; init; }

    /// <summary>Код банка СБП для возврата.</summary>
    public string? QrMemberId { get; init; }

    /// <summary>Идентификатор операции на стороне мерчанта.</summary>
    public string? ExternalRequestId { get; init; }
}

/// <summary>
/// Запрос Confirm для подтверждения авторизованного платежа.
/// </summary>
public sealed record TBankConfirmPaymentRequest
{
    /// <summary>Ключ терминала. Обычно заполняется клиентом автоматически.</summary>
    public string? TerminalKey { get; init; }

    /// <summary>Идентификатор платежа в T-Bank.</summary>
    public required string PaymentId { get; init; }

    /// <summary>Подпись запроса. Обычно генерируется клиентом автоматически.</summary>
    public string? Token { get; init; }

    /// <summary>IP-адрес клиента.</summary>
    public string? IP { get; init; }

    /// <summary>Сумма подтверждения.</summary>
    [JsonConverter(typeof(TBankAmountJsonConverter))]
    public TBankAmount? Amount { get; init; }
}

/// <summary>
/// Запрос GetQr для получения QR payload или изображения.
/// </summary>
public sealed record TBankQrRequest
{
    /// <summary>Ключ терминала. Обычно заполняется клиентом автоматически.</summary>
    public string? TerminalKey { get; init; }

    /// <summary>Идентификатор платежа в T-Bank.</summary>
    public required string PaymentId { get; init; }

    /// <summary>Формат данных QR.</summary>
    [JsonConverter(typeof(TBankQrDataTypeJsonConverter))]
    public TBankQrDataType? DataType { get; init; }

    /// <summary>Подпись запроса. Обычно генерируется клиентом автоматически.</summary>
    public string? Token { get; init; }
}

/// <summary>
/// Запрос GetQrState для получения статуса возврата платежа по СБП.
/// </summary>
public sealed record TBankQrStateRequest
{
    /// <summary>Ключ терминала. Обычно заполняется клиентом автоматически.</summary>
    public string? TerminalKey { get; init; }

    /// <summary>Идентификатор платежа в T-Bank.</summary>
    public required string PaymentId { get; init; }

    /// <summary>Подпись запроса. Обычно генерируется клиентом автоматически.</summary>
    public string? Token { get; init; }
}

/// <summary>
/// Запрос GetQrBankList для получения списка банков-участников СБП.
/// </summary>
public sealed record TBankQrBankListRequest
{
    /// <summary>Ключ терминала. Обычно заполняется клиентом автоматически.</summary>
    public string? TerminalKey { get; init; }

    /// <summary>Тип сценария оплаты: qr — оплата, sub — привязка счета. По умолчанию qr.</summary>
    public string? ScenarioType { get; init; }

    /// <summary>Тип и ОС устройства покупателя.</summary>
    public required TBankQrDevice Device { get; init; }

    /// <summary>Подпись запроса. Обычно генерируется клиентом автоматически.</summary>
    public string? Token { get; init; }
}

/// <summary>
/// Тип и ОС устройства покупателя для запроса GetQrBankList.
/// </summary>
public sealed record TBankQrDevice
{
    /// <summary>Тип устройства: desktop или mobile.</summary>
    public required string Type { get; init; }

    /// <summary>ОС устройства, например iOS или Android.</summary>
    public required string Os { get; init; }
}

/// <summary>
/// Запрос GetAccountQrList для получения списка привязанных к магазину счетов.
/// </summary>
public sealed record TBankAccountQrListRequest
{
    /// <summary>Ключ терминала. Обычно заполняется клиентом автоматически.</summary>
    public string? TerminalKey { get; init; }

    /// <summary>Подпись запроса. Обычно генерируется клиентом автоматически.</summary>
    public string? Token { get; init; }
}

/// <summary>
/// Запрос AddAccountQr для привязки счета покупателя к магазину.
/// </summary>
public sealed record TBankAddAccountQrRequest
{
    /// <summary>Ключ терминала. Обычно заполняется клиентом автоматически.</summary>
    public string? TerminalKey { get; init; }

    /// <summary>Подробное описание деталей заказа.</summary>
    public required string Description { get; init; }

    /// <summary>Тип возвращаемых данных: PAYLOAD или IMAGE. По умолчанию PAYLOAD.</summary>
    [JsonConverter(typeof(TBankQrDataTypeJsonConverter))]
    public TBankQrDataType? DataType { get; init; }

    /// <summary>Внутренний идентификатор выбранного банка. Передается только для DataType = PAYLOAD.</summary>
    public string? BankId { get; init; }

    /// <summary>Дополнительные параметры платежной страницы в виде ключ:значение (не более 20 пар).</summary>
    public IReadOnlyDictionary<string, string?>? Data { get; init; }

    /// <summary>Срок жизни ссылки или динамического QR.</summary>
    public DateTimeOffset? RedirectDueDate { get; init; }

    /// <summary>Подпись запроса. Обычно генерируется клиентом автоматически.</summary>
    public string? Token { get; init; }
}

/// <summary>
/// Запрос GetAddAccountQrState для получения статуса привязки счета к магазину.
/// </summary>
public sealed record TBankAddAccountQrStateRequest
{
    /// <summary>Ключ терминала. Обычно заполняется клиентом автоматически.</summary>
    public string? TerminalKey { get; init; }

    /// <summary>Идентификатор запроса на привязку счета.</summary>
    public required string RequestKey { get; init; }

    /// <summary>Подпись запроса. Обычно генерируется клиентом автоматически.</summary>
    public string? Token { get; init; }
}

/// <summary>
/// Запрос QrMembersList для получения списка банков-участников QR для возврата.
/// </summary>
public sealed record TBankQrMembersListRequest
{
    /// <summary>Ключ терминала. Обычно заполняется клиентом автоматически.</summary>
    public string? TerminalKey { get; init; }

    /// <summary>Идентификатор платежа в T-Bank.</summary>
    public required string PaymentId { get; init; }

    /// <summary>Подпись запроса. Обычно генерируется клиентом автоматически.</summary>
    public string? Token { get; init; }
}

/// <summary>
/// Запрос SendClosingReceipt для отправки закрывающего чека в кассу (ФФД 1.2).
/// </summary>
public sealed record TBankSendClosingReceiptFfd12Request
{
    /// <summary>Ключ терминала. Обычно заполняется клиентом автоматически.</summary>
    public string? TerminalKey { get; init; }

    /// <summary>Идентификатор платежа в T-Bank.</summary>
    public required string PaymentId { get; init; }

    /// <summary>Данные фискального чека формата ФФД 1.2.</summary>
    public required TBankReceiptFfd12 Receipt { get; init; }

    /// <summary>Подпись запроса. Обычно генерируется клиентом автоматически.</summary>
    public string? Token { get; init; }
}

/// <summary>
/// Запрос SendClosingReceipt для отправки закрывающего чека в кассу (ФФД 1.05).
/// </summary>
public sealed record TBankSendClosingReceiptFfd105Request
{
    /// <summary>Ключ терминала. Обычно заполняется клиентом автоматически.</summary>
    public string? TerminalKey { get; init; }

    /// <summary>Идентификатор платежа в T-Bank.</summary>
    public required string PaymentId { get; init; }

    /// <summary>Данные фискального чека формата ФФД 1.05.</summary>
    public required TBankReceiptFfd105 Receipt { get; init; }

    /// <summary>Подпись запроса. Обычно генерируется клиентом автоматически.</summary>
    public string? Token { get; init; }
}

/// <summary>
/// Запрос MirPay/GetDeepLink для получения подписанного DeepLink Mir Pay.
/// </summary>
public sealed record TBankMirPayDeepLinkRequest
{
    /// <summary>Ключ терминала. Обычно заполняется клиентом автоматически.</summary>
    public string? TerminalKey { get; init; }

    /// <summary>Идентификатор платежа в T-Bank.</summary>
    public required string PaymentId { get; init; }

    /// <summary>Подпись запроса. Обычно генерируется клиентом автоматически.</summary>
    public string? Token { get; init; }
}

/// <summary>
/// Запрос AlfaPay/link/get для получения ссылки Alfa Pay.
/// </summary>
public sealed record TBankAlfaPayLinkRequest
{
    /// <summary>Ключ терминала. Обычно заполняется клиентом автоматически.</summary>
    public string? TerminalKey { get; init; }

    /// <summary>Идентификатор платежа в T-Bank.</summary>
    public required string PaymentId { get; init; }

    /// <summary>Подпись запроса. Обычно генерируется клиентом автоматически.</summary>
    public string? Token { get; init; }
}

/// <summary>
/// Запрос SbpPayTest для создания тестовой платежной сессии СБП.
/// </summary>
/// <remarks>Работает только на тестовом терминале и эмулирует сценарии проведения платежа.</remarks>
public sealed record TBankSbpPayTestRequest
{
    /// <summary>Ключ терминала. Обычно заполняется клиентом автоматически.</summary>
    public string? TerminalKey { get; init; }

    /// <summary>Идентификатор платежа в T-Bank.</summary>
    public required string PaymentId { get; init; }

    /// <summary>Подпись запроса. Обычно генерируется клиентом автоматически.</summary>
    public string? Token { get; init; }

    /// <summary>Эмулировать отказ банка по таймауту. Нельзя использовать вместе с IsRejected.</summary>
    public bool? IsDeadlineExpired { get; init; }

    /// <summary>Эмулировать отказ банка в проведении платежа. Нельзя использовать вместе с IsDeadlineExpired.</summary>
    public bool? IsRejected { get; init; }
}

/// <summary>
/// Запрос ChargeQr для списания по привязанному QR-счету.
/// </summary>
public sealed record TBankChargeQrRequest
{
    /// <summary>Ключ терминала. Обычно заполняется клиентом автоматически.</summary>
    public string? TerminalKey { get; init; }

    /// <summary>Идентификатор платежа в T-Bank.</summary>
    public required string PaymentId { get; init; }

    /// <summary>Идентификатор привязки счета.</summary>
    public required string AccountToken { get; init; }

    /// <summary>Подпись запроса. Обычно генерируется клиентом автоматически.</summary>
    public string? Token { get; init; }

    /// <summary>IP-адрес клиента.</summary>
    public string? IP { get; init; }

    /// <summary>Отправить email-уведомление покупателю.</summary>
    public bool? SendEmail { get; init; }

    /// <summary>Email покупателя для уведомления.</summary>
    public string? InfoEmail { get; init; }

    /// <summary>Идентификатор банка плательщика. Обязателен для привязки карты другого банка.</summary>
    public string? BankMemberId { get; init; }
}
