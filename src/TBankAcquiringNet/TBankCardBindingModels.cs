using System.Text.Json.Serialization;

namespace TBankAcquiringNet;

/// <summary>
/// Запрос AttachCard для завершения привязки карты на собственной платежной форме мерчанта.
/// </summary>
/// <remarks>
/// Только для мерчантов с собственной формой (PCI DSS). <see cref="CardData"/> содержит зашифрованные
/// данные карты — не логируйте и не сохраняйте это значение. Для 3DS v2.1 предварительно вызовите
/// Check3dsVersion и этап 3DS Method.
/// </remarks>
public sealed record TBankAttachCardRequest
{
    /// <summary>Ключ терминала. Обычно заполняется клиентом автоматически.</summary>
    public string? TerminalKey { get; init; }

    /// <summary>Идентификатор запроса на привязку карты.</summary>
    public required string RequestKey { get; init; }

    /// <summary>
    /// Зашифрованные данные карты (PCI). Открытый шаблон PAN=...;ExpDate=...;CardHolder=...;CVV=...
    /// должен быть зашифрован публичным ключом терминала (RSA) и закодирован в Base64 перед отправкой —
    /// не передавайте, не логируйте и не сохраняйте открытые реквизиты.
    /// </summary>
    public required string CardData { get; init; }

    /// <summary>Канал устройства: 01 — Application (APP), 02 — Browser (BRW). По умолчанию 02.</summary>
    [JsonPropertyName("deviceChannel")]
    public string DeviceChannel { get; init; } = "02";

    /// <summary>Дополнительные параметры операции (для 3DS v2.1) в виде ключ:значение.</summary>
    public IReadOnlyDictionary<string, string?>? DATA { get; init; }

    /// <summary>Подпись запроса. Обычно генерируется клиентом автоматически.</summary>
    public string? Token { get; init; }
}

/// <summary>
/// Ответ метода AttachCard.
/// </summary>
public sealed record TBankAttachCardResponse : TBankResponse
{
    /// <summary>Ключ терминала.</summary>
    public string? TerminalKey { get; init; }

    /// <summary>Идентификатор покупателя в системе мерчанта.</summary>
    public string? CustomerKey { get; init; }

    /// <summary>Идентификатор запроса на привязку карты.</summary>
    public string? RequestKey { get; init; }

    /// <summary>Идентификатор карты в системе T-Bank.</summary>
    public string? CardId { get; init; }

    /// <summary>Статус привязки карты.</summary>
    [JsonConverter(typeof(TBankPaymentStatusJsonConverter))]
    public TBankPaymentStatus Status { get; init; }

    /// <summary>Идентификатор сохраненных реквизитов карты (для рекуррентных платежей).</summary>
    public string? RebillId { get; init; }

    /// <summary>Адрес сервера ACS для проверки 3DS. Возвращается для статуса 3DS_CHECKING.</summary>
    public string? ACSUrl { get; init; }

    /// <summary>Идентификатор платежа в ACS. Возвращается для статуса 3DS_CHECKING.</summary>
    public string? MD { get; init; }

    /// <summary>Запрос PaReq для отправки на ACSUrl (3DS v1.0). Возвращается для статуса 3DS_CHECKING.</summary>
    public string? PaReq { get; init; }
}
