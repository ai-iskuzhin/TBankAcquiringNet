using System.Text.Json.Serialization;

namespace TBankAcquiringNet;

/// <summary>
/// Запрос Charge для проведения COF-операции по сохраненным реквизитам карты (RebillId).
/// </summary>
public sealed record TBankChargeRequest
{
    /// <summary>Ключ терминала. Обычно заполняется клиентом автоматически.</summary>
    public string? TerminalKey { get; init; }

    /// <summary>Идентификатор платежа в T-Bank.</summary>
    public required string PaymentId { get; init; }

    /// <summary>Идентификатор сохраненных реквизитов карты покупателя.</summary>
    public required string RebillId { get; init; }

    /// <summary>Подпись запроса. Обычно генерируется клиентом автоматически.</summary>
    public string? Token { get; init; }

    /// <summary>IP-адрес покупателя.</summary>
    public string? IP { get; init; }

    /// <summary>Отправить email-уведомление об оплате покупателю.</summary>
    public bool? SendEmail { get; init; }

    /// <summary>Email покупателя. Обязателен, если передан SendEmail=true.</summary>
    public string? InfoEmail { get; init; }
}

/// <summary>
/// Ответ метода Charge.
/// </summary>
public sealed record TBankChargeResponse : TBankResponse
{
    /// <summary>Ключ терминала.</summary>
    public string? TerminalKey { get; init; }

    /// <summary>Сумма операции.</summary>
    [JsonConverter(typeof(TBankAmountJsonConverter))]
    public TBankAmount? Amount { get; init; }

    /// <summary>Номер заказа на стороне площадки.</summary>
    public string? OrderId { get; init; }

    /// <summary>Статус платежа: CONFIRMED — выполнен, REJECTED — не выполнен.</summary>
    [JsonConverter(typeof(TBankPaymentStatusJsonConverter))]
    public TBankPaymentStatus Status { get; init; }

    /// <summary>Идентификатор платежа в системе T-Bank.</summary>
    [JsonConverter(typeof(TBankStringJsonConverter))]
    public string? PaymentId { get; init; }
}
