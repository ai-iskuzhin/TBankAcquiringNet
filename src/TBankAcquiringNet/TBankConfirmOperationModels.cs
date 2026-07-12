using System.Text.Json.Serialization;

namespace TBankAcquiringNet;

/// <summary>
/// Запрос getConfirmOperation для генерации справки по операциям (работает по карте, T-Pay и Mir Pay).
/// </summary>
/// <remarks>Token формируется только из Password и TerminalKey.</remarks>
public sealed record TBankGetConfirmOperationRequest
{
    /// <summary>Ключ терминала. Обычно заполняется клиентом автоматически.</summary>
    public string? TerminalKey { get; init; }

    /// <summary>
    /// URL сервиса получения справок. Укажите либо <see cref="CallbackUrl"/>, либо
    /// <see cref="EmailList"/> — один из способов доставки справки.
    /// </summary>
    public string? CallbackUrl { get; init; }

    /// <summary>
    /// Список адресов для доставки справки по email (до трёх адресов). Альтернатива
    /// <see cref="CallbackUrl"/>.
    /// </summary>
    public IReadOnlyList<TBankConfirmOperationEmail>? EmailList { get; init; }

    /// <summary>Перечень идентификаторов платежей (PaymentId) для справки.</summary>
    public required IReadOnlyList<long> PaymentIdList { get; init; }

    /// <summary>Подпись запроса. Обычно генерируется клиентом автоматически.</summary>
    public string? Token { get; init; }
}

/// <summary>
/// Адрес электронной почты для доставки справки getConfirmOperation.
/// </summary>
public sealed record TBankConfirmOperationEmail
{
    /// <summary>Адрес электронной почты получателя справки.</summary>
    public required string Email { get; init; }
}

/// <summary>
/// Ответ метода getConfirmOperation.
/// </summary>
/// <remarks>
/// Не наследует <see cref="TBankResponse"/>: T-Bank возвращает <c>ErrorCode</c> числом, поэтому оно
/// разбирается через устойчивый строковый конвертер. Проверяйте <see cref="Success"/> у ответа
/// и у каждого элемента <see cref="PaymentIdList"/>.
/// </remarks>
public sealed record TBankGetConfirmOperationResponse
{
    /// <summary>Успешность прохождения запроса.</summary>
    public bool Success { get; init; }

    /// <summary>Код ошибки.</summary>
    [JsonConverter(typeof(TBankStringJsonConverter))]
    public string? ErrorCode { get; init; }

    /// <summary>Краткое описание ошибки.</summary>
    public string? Message { get; init; }

    /// <summary>Результаты по каждому запрошенному платежу.</summary>
    public IReadOnlyList<TBankConfirmOperationResult> PaymentIdList { get; init; } = [];
}

/// <summary>
/// Результат справки по одному платежу в ответе getConfirmOperation.
/// </summary>
public sealed record TBankConfirmOperationResult
{
    /// <summary>Успешность обработки по данному платежу.</summary>
    public bool Success { get; init; }

    /// <summary>Код ошибки.</summary>
    [JsonConverter(typeof(TBankStringJsonConverter))]
    public string? ErrorCode { get; init; }

    /// <summary>Сервисное сообщение.</summary>
    public string? Message { get; init; }

    /// <summary>Идентификатор платежа в системе T-Bank.</summary>
    [JsonConverter(typeof(TBankStringJsonConverter))]
    public string? PaymentId { get; init; }
}
