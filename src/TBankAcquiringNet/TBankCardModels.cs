using System.Text.Json.Serialization;

namespace TBankAcquiringNet;

/// <summary>
/// Запрос AddCard для инициации привязки карты к покупателю через форму привязки банка.
/// </summary>
public sealed record TBankAddCardRequest
{
    /// <summary>Ключ терминала. Обычно заполняется клиентом автоматически.</summary>
    public string? TerminalKey { get; init; }

    /// <summary>Идентификатор покупателя в системе мерчанта.</summary>
    public required string CustomerKey { get; init; }

    /// <summary>Подпись запроса. Обычно генерируется клиентом автоматически.</summary>
    public string? Token { get; init; }

    /// <summary>
    /// Тип проверки при сохранении карты: NO, HOLD, 3DS, 3DSHOLD. По умолчанию NO.
    /// </summary>
    public string? CheckType { get; init; }

    /// <summary>IP-адрес покупателя.</summary>
    public string? IP { get; init; }

    /// <summary>Признак резидентности карты: true — РФ, false — не РФ, null — не специфицируется.</summary>
    public bool? ResidentState { get; init; }
}

/// <summary>
/// Запрос GetAddCardState для получения статуса привязки карты.
/// </summary>
public sealed record TBankGetAddCardStateRequest
{
    /// <summary>Ключ терминала. Обычно заполняется клиентом автоматически.</summary>
    public string? TerminalKey { get; init; }

    /// <summary>Идентификатор запроса на привязку карты.</summary>
    public required string RequestKey { get; init; }

    /// <summary>Подпись запроса. Обычно генерируется клиентом автоматически.</summary>
    public string? Token { get; init; }
}

/// <summary>
/// Запрос GetCardList для получения списка привязанных карт покупателя.
/// </summary>
public sealed record TBankGetCardListRequest
{
    /// <summary>Ключ терминала. Обычно заполняется клиентом автоматически.</summary>
    public string? TerminalKey { get; init; }

    /// <summary>Идентификатор покупателя в системе мерчанта.</summary>
    public required string CustomerKey { get; init; }

    /// <summary>Признак сохранения карты для оплаты в один клик.</summary>
    public bool? SavedCard { get; init; }

    /// <summary>Подпись запроса. Обычно генерируется клиентом автоматически.</summary>
    public string? Token { get; init; }

    /// <summary>IP-адрес покупателя.</summary>
    public string? IP { get; init; }
}

/// <summary>
/// Запрос RemoveCard для удаления привязанной карты покупателя.
/// </summary>
public sealed record TBankRemoveCardRequest
{
    /// <summary>Ключ терминала. Обычно заполняется клиентом автоматически.</summary>
    public string? TerminalKey { get; init; }

    /// <summary>Идентификатор покупателя в системе мерчанта.</summary>
    public required string CustomerKey { get; init; }

    /// <summary>Идентификатор карты в системе T-Bank.</summary>
    public required string CardId { get; init; }

    /// <summary>Подпись запроса. Обычно генерируется клиентом автоматически.</summary>
    public string? Token { get; init; }

    /// <summary>IP-адрес покупателя.</summary>
    public string? IP { get; init; }
}

/// <summary>
/// Ответ метода AddCard.
/// </summary>
public sealed record TBankAddCardResponse : TBankResponse
{
    /// <summary>Идентификатор платежа в системе T-Bank.</summary>
    [JsonConverter(typeof(TBankStringJsonConverter))]
    public string? PaymentId { get; init; }

    /// <summary>Ключ терминала.</summary>
    public string? TerminalKey { get; init; }

    /// <summary>Идентификатор покупателя в системе мерчанта.</summary>
    public string? CustomerKey { get; init; }

    /// <summary>Идентификатор запроса на привязку карты.</summary>
    public string? RequestKey { get; init; }

    /// <summary>Ссылка на форму привязки карты T-Bank.</summary>
    public string? PaymentURL { get; init; }
}

/// <summary>
/// Ответ метода GetAddCardState.
/// </summary>
public sealed record TBankGetAddCardStateResponse : TBankResponse
{
    /// <summary>Ключ терминала.</summary>
    public string? TerminalKey { get; init; }

    /// <summary>Идентификатор запроса на привязку карты.</summary>
    public string? RequestKey { get; init; }

    /// <summary>Статус привязки карты.</summary>
    [JsonConverter(typeof(TBankPaymentStatusJsonConverter))]
    public TBankPaymentStatus Status { get; init; }

    /// <summary>Идентификатор карты в системе T-Bank.</summary>
    public string? CardId { get; init; }

    /// <summary>Идентификатор сохраненных реквизитов карты (для рекуррентных платежей).</summary>
    public string? RebillId { get; init; }

    /// <summary>Идентификатор покупателя в системе мерчанта.</summary>
    public string? CustomerKey { get; init; }
}

/// <summary>
/// Ответ метода RemoveCard.
/// </summary>
public sealed record TBankRemoveCardResponse : TBankResponse
{
    /// <summary>Ключ терминала.</summary>
    public string? TerminalKey { get; init; }

    /// <summary>Статус карты (D — удалена).</summary>
    [JsonConverter(typeof(TBankCardStatusJsonConverter))]
    public TBankCardStatus Status { get; init; }

    /// <summary>Идентификатор покупателя в системе мерчанта.</summary>
    public string? CustomerKey { get; init; }

    /// <summary>Идентификатор карты в системе T-Bank.</summary>
    public string? CardId { get; init; }

    /// <summary>Тип карты: 0 — списание, 1 — пополнение, 2 — пополнение и списание.</summary>
    [JsonConverter(typeof(TBankInt32JsonConverter))]
    public int? CardType { get; init; }
}

/// <summary>
/// Привязанная карта покупателя в ответе GetCardList.
/// </summary>
public sealed record TBankCard
{
    /// <summary>Идентификатор карты в системе T-Bank.</summary>
    public string? CardId { get; init; }

    /// <summary>Маскированный номер карты.</summary>
    public string? Pan { get; init; }

    /// <summary>Статус карты: A — активная, D — удаленная.</summary>
    [JsonConverter(typeof(TBankCardStatusJsonConverter))]
    public TBankCardStatus Status { get; init; }

    /// <summary>Идентификатор сохраненных реквизитов карты (для рекуррентных платежей).</summary>
    public string? RebillId { get; init; }

    /// <summary>Тип карты: 0 — списание, 1 — пополнение, 2 — пополнение и списание.</summary>
    [JsonConverter(typeof(TBankInt32JsonConverter))]
    public int? CardType { get; init; }

    /// <summary>Срок действия карты.</summary>
    public string? ExpDate { get; init; }
}
