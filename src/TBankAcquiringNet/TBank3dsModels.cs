using System.Text.Json.Serialization;

namespace TBankAcquiringNet;

/// <summary>
/// Запрос Check3dsVersion для определения версии протокола 3DS по карте.
/// </summary>
/// <remarks>
/// Только для мерчантов с собственной платежной формой (PCI DSS). <see cref="CardData"/> содержит
/// зашифрованные данные карты — не логируйте и не сохраняйте это значение.
/// </remarks>
public sealed record TBankCheck3dsVersionRequest
{
    /// <summary>Идентификатор платежа в T-Bank.</summary>
    public required string PaymentId { get; init; }

    /// <summary>Ключ терминала. Обычно заполняется клиентом автоматически.</summary>
    public string? TerminalKey { get; init; }

    /// <summary>Зашифрованные данные карты (PCI).</summary>
    public required string CardData { get; init; }

    /// <summary>Подпись запроса. Обычно генерируется клиентом автоматически.</summary>
    public string? Token { get; init; }
}

/// <summary>
/// Ответ метода Check3dsVersion.
/// </summary>
public sealed record TBankCheck3dsVersionResponse : TBankResponse
{
    /// <summary>Версия протокола 3DS, например 1.0.0 или 2.1.0.</summary>
    public string? Version { get; init; }

    /// <summary>Идентификатор платежа, сгенерированный 3DS Server (обязателен для 3DS v2.1).</summary>
    public string? TdsServerTransID { get; init; }

    /// <summary>URL этапа 3DS Method для 3DS v2.1.</summary>
    public string? ThreeDSMethodURL { get; init; }

    /// <summary>Платежная система карты.</summary>
    public string? PaymentSystem { get; init; }
}

/// <summary>
/// Запрос Submit3DSAuthorization для подтверждения прохождения 3DS v1.0 (x-www-form-urlencoded).
/// </summary>
public sealed record TBankSubmit3DSAuthorizationRequest
{
    /// <summary>Идентификатор платежа из ответа ACS.</summary>
    public required string MD { get; init; }

    /// <summary>Результат 3D Secure аутентификации из ответа ACS.</summary>
    public required string PaRes { get; init; }

    /// <summary>Идентификатор платежа в T-Bank.</summary>
    public string? PaymentId { get; init; }

    /// <summary>Ключ терминала. Обычно заполняется клиентом автоматически.</summary>
    public string? TerminalKey { get; init; }

    /// <summary>Подпись запроса. Обычно генерируется клиентом автоматически.</summary>
    public string? Token { get; init; }
}

/// <summary>
/// Запрос Submit3DSAuthorizationV2 для подтверждения прохождения 3DS v2.1 (x-www-form-urlencoded).
/// </summary>
public sealed record TBankSubmit3DSAuthorizationV2Request
{
    /// <summary>Идентификатор платежа в T-Bank.</summary>
    public required string PaymentId { get; init; }

    /// <summary>Ключ терминала. Обычно заполняется клиентом автоматически.</summary>
    public string? TerminalKey { get; init; }

    /// <summary>Подпись запроса. Обычно генерируется клиентом автоматически.</summary>
    public string? Token { get; init; }
}

/// <summary>
/// Ответ методов Submit3DSAuthorization и Submit3DSAuthorizationV2.
/// </summary>
public sealed record TBankSubmit3DSAuthorizationResponse : TBankResponse
{
    /// <summary>Ключ терминала.</summary>
    public string? TerminalKey { get; init; }

    /// <summary>Номер заказа на стороне площадки.</summary>
    public string? OrderId { get; init; }

    /// <summary>Статус транзакции: CONFIRMED, AUTHORIZED или REJECTED.</summary>
    [JsonConverter(typeof(TBankPaymentStatusJsonConverter))]
    public TBankPaymentStatus Status { get; init; }

    /// <summary>Идентификатор платежа в системе T-Bank.</summary>
    [JsonConverter(typeof(TBankStringJsonConverter))]
    public string? PaymentId { get; init; }
}
