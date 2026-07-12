namespace TBankAcquiringNet;

/// <summary>
/// Настройки клиента платежного API T-Bank.
/// </summary>
/// <example>
/// <code>
/// var client = new TBankPaymentsClient(httpClient, new TBankPaymentsClientOptions
/// {
///     TerminalKey = "TinkoffBankTest",
///     Password = "...",
///     BaseAddress = new Uri("https://securepay.tinkoff.ru/v2/")
/// });
/// </code>
/// </example>
public sealed class TBankPaymentsClientOptions
{
    /// <summary>Ключ терминала, выданный T-Bank.</summary>
    /// <value>Передается в каждый запрос как TerminalKey.</value>
    public required string TerminalKey { get; init; }

    /// <summary>Пароль терминала для генерации Token.</summary>
    /// <value>Не отправляется как поле запроса; используется локально для подписи.</value>
    public required string Password { get; init; }

    /// <summary>Среда API, используемая при отсутствии BaseAddress.</summary>
    /// <value>По умолчанию <see cref="TBankAcquiringEnvironment.Production"/>.</value>
    public TBankAcquiringEnvironment Environment { get; init; } = TBankAcquiringEnvironment.Production;

    /// <summary>Явный базовый URL API, если нужно переопределить среду.</summary>
    /// <value>Например https://securepay.tinkoff.ru/v2/.</value>
    public Uri? BaseAddress { get; init; }

    /// <summary>Автоматически генерировать Token перед отправкой запросов.</summary>
    /// <value>true по умолчанию. Отключайте только для низкоуровневой диагностики.</value>
    public bool AutoGenerateToken { get; init; } = true;

    /// <summary>Выбрасывать исключение при ответе T-Bank с Success=false.</summary>
    /// <value>false по умолчанию: API-ошибки T-Bank возвращаются типизированным ответом.</value>
    public bool ThrowOnTBankApiError { get; init; }

    /// <summary>Сохранять сырое тело ответа в Metadata.RawResponseBody.</summary>
    /// <value>false по умолчанию, чтобы случайно не хранить чувствительные данные.</value>
    /// <remarks>
    /// Тело сохраняется как есть, без маскирования: оно может содержать Token, RebillId и другие
    /// чувствительные поля ответа. Включайте только для отладки и не логируйте результат в открытом виде.
    /// </remarks>
    public bool CaptureRawResponseBody { get; init; }

    internal Uri ResolveBaseAddress()
    {
        if (BaseAddress is not null)
        {
            return BaseAddress;
        }

        return Environment switch
        {
            TBankAcquiringEnvironment.Test => new Uri("https://rest-api-test.tinkoff.ru/v2/"),
            TBankAcquiringEnvironment.Production => new Uri("https://securepay.tinkoff.ru/v2/"),
            _ => throw new InvalidOperationException($"Unsupported T-Bank acquiring environment: {Environment}.")
        };
    }
}
