using System.Text.Json.Serialization;

namespace TBankAcquiringNet.Payments;

/// <summary>
/// HTTP-нотификация T-Bank о статусе платежа.
/// </summary>
public sealed record TBankPaymentNotification
{
    /// <summary>Ключ терминала.</summary>
    public required string TerminalKey { get; init; }

    /// <summary>Номер заказа на стороне площадки.</summary>
    public required string OrderId { get; init; }

    /// <summary>Признак успешности операции.</summary>
    public bool Success { get; init; }

    /// <summary>Статус платежа.</summary>
    [JsonConverter(typeof(TBankPaymentStatusJsonConverter))]
    public TBankPaymentStatus Status { get; init; }

    /// <summary>Идентификатор платежа в T-Bank.</summary>
    [JsonConverter(typeof(TBankStringJsonConverter))]
    public required string PaymentId { get; init; }

    /// <summary>Код ошибки T-Bank.</summary>
    public required string ErrorCode { get; init; }

    /// <summary>Текущая сумма операции.</summary>
    [JsonConverter(typeof(TBankAmountJsonConverter))]
    public TBankAmount? Amount { get; init; }

    /// <summary>Идентификатор привязанной карты.</summary>
    [JsonConverter(typeof(TBankStringJsonConverter))]
    public string? CardId { get; init; }

    /// <summary>Маскированный номер карты или телефона.</summary>
    public string? Pan { get; init; }

    /// <summary>Срок действия карты.</summary>
    public string? ExpDate { get; init; }

    /// <summary>Идентификатор рекуррентного платежа.</summary>
    public string? RebillId { get; init; }

    /// <summary>Подпись нотификации.</summary>
    public required string Token { get; init; }

    /// <summary>Дополнительные параметры платежа.</summary>
    public string? DATA { get; init; }

    /// <summary>Идентификатор сделки.</summary>
    public string? SpAccumulationId { get; init; }
}
