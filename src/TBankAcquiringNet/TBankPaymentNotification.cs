using System.Text.Json;
using System.Text.Json.Serialization;

namespace TBankAcquiringNet;

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
    [JsonConverter(typeof(TBankNotificationPaymentStatusJsonConverter))]
    public TBankPaymentStatus Status { get; init; }

    /// <summary>Статус так, как его прислал банк.</summary>
    /// <remarks>
    /// Заполняется разбором через <see cref="TBankPaymentNotificationValidator"/>. Незнакомый банку
    /// статус попадает в <see cref="Status"/> как <see cref="TBankPaymentStatus.UNKNOWN"/>, но здесь
    /// остаётся дословно: событие о деньгах не теряется из-за статуса, которого SDK ещё не знает.
    /// </remarks>
    [JsonIgnore]
    public string? StatusRaw { get; init; }

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
    /// <remarks>
    /// Банк присылает вложенный объект; здесь он сохраняется как исходный JSON. В подпись не входит:
    /// подписываются только скалярные поля верхнего уровня.
    /// </remarks>
    [JsonConverter(typeof(TBankRawJsonJsonConverter))]
    [TBankUnsignedField]
    public string? DATA { get; init; }

    /// <summary>Идентификатор сделки.</summary>
    public string? SpAccumulationId { get; init; }

    /// <summary>Поля верхнего уровня, которых нет в модели.</summary>
    /// <remarks>
    /// Банк подписывает <em>все</em> присланные поля верхнего уровня, а не только известные SDK.
    /// Поле, оставшееся за моделью, выпало бы из подписи, и нотификация получила бы
    /// <see cref="TBankPaymentNotificationValidationResult.InvalidToken"/>, — поэтому неизвестные
    /// поля сохраняются и проверяются наравне с остальными.
    /// </remarks>
    [JsonExtensionData]
    public Dictionary<string, JsonElement>? AdditionalFields { get; init; }

    /// <summary>Скалярные поля верхнего уровня так, как они пришли, кроме <c>Token</c>.</summary>
    /// <remarks>
    /// Заполняется разбором через <see cref="TBankPaymentNotificationValidator"/> и используется для
    /// проверки подписи. Это и есть единственный способ подписать ровно то, что прислал банк: модель
    /// может не знать поля, округлить число или не разобрать статус, а подпись считается по проводу.
    /// </remarks>
    [JsonIgnore]
    public IReadOnlyDictionary<string, string>? RawFields { get; init; }
}
