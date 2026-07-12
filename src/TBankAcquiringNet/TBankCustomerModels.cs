namespace TBankAcquiringNet;

/// <summary>
/// Запрос AddCustomer для регистрации покупателя в связке с терминалом.
/// </summary>
public sealed record TBankAddCustomerRequest
{
    /// <summary>Ключ терминала. Обычно заполняется клиентом автоматически.</summary>
    public string? TerminalKey { get; init; }

    /// <summary>Идентификатор покупателя в системе мерчанта.</summary>
    public required string CustomerKey { get; init; }

    /// <summary>Подпись запроса. Обычно генерируется клиентом автоматически.</summary>
    public string? Token { get; init; }

    /// <summary>IP-адрес покупателя.</summary>
    public string? IP { get; init; }

    /// <summary>Электронная почта покупателя.</summary>
    public string? Email { get; init; }

    /// <summary>Телефон покупателя в формате +{Ц}.</summary>
    public string? Phone { get; init; }
}

/// <summary>
/// Запрос GetCustomer для получения данных покупателя.
/// </summary>
public sealed record TBankGetCustomerRequest
{
    /// <summary>Ключ терминала. Обычно заполняется клиентом автоматически.</summary>
    public string? TerminalKey { get; init; }

    /// <summary>Идентификатор покупателя в системе мерчанта.</summary>
    public required string CustomerKey { get; init; }

    /// <summary>Подпись запроса. Обычно генерируется клиентом автоматически.</summary>
    public string? Token { get; init; }

    /// <summary>IP-адрес покупателя.</summary>
    public string? IP { get; init; }
}

/// <summary>
/// Запрос RemoveCustomer для удаления сохраненных данных покупателя.
/// </summary>
public sealed record TBankRemoveCustomerRequest
{
    /// <summary>Ключ терминала. Обычно заполняется клиентом автоматически.</summary>
    public string? TerminalKey { get; init; }

    /// <summary>Идентификатор покупателя в системе мерчанта.</summary>
    public required string CustomerKey { get; init; }

    /// <summary>Подпись запроса. Обычно генерируется клиентом автоматически.</summary>
    public string? Token { get; init; }

    /// <summary>IP-адрес покупателя.</summary>
    public string? IP { get; init; }
}

/// <summary>
/// Ответ метода AddCustomer.
/// </summary>
public sealed record TBankAddCustomerResponse : TBankResponse
{
    /// <summary>Ключ терминала.</summary>
    public string? TerminalKey { get; init; }

    /// <summary>Идентификатор покупателя в системе мерчанта.</summary>
    public string? CustomerKey { get; init; }
}

/// <summary>
/// Ответ метода GetCustomer.
/// </summary>
public sealed record TBankGetCustomerResponse : TBankResponse
{
    /// <summary>Ключ терминала.</summary>
    public string? TerminalKey { get; init; }

    /// <summary>Идентификатор покупателя в системе мерчанта.</summary>
    public string? CustomerKey { get; init; }

    /// <summary>Электронная почта покупателя.</summary>
    public string? Email { get; init; }

    /// <summary>Телефон покупателя.</summary>
    public string? Phone { get; init; }
}

/// <summary>
/// Ответ метода RemoveCustomer.
/// </summary>
public sealed record TBankRemoveCustomerResponse : TBankResponse
{
    /// <summary>Ключ терминала.</summary>
    public string? TerminalKey { get; init; }

    /// <summary>Идентификатор покупателя в системе мерчанта.</summary>
    public string? CustomerKey { get; init; }
}
