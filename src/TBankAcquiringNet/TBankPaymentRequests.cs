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
    public string? Recurrent { get; init; }

    /// <summary>Тип оплаты.</summary>
    public string? PayType { get; init; }

    /// <summary>Язык платежной формы.</summary>
    public string? Language { get; init; }

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
}
