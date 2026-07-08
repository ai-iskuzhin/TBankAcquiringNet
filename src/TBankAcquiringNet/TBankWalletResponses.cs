namespace TBankAcquiringNet;

/// <summary>
/// Ответ метода проверки доступности T‑Pay (TinkoffPay status).
/// </summary>
public sealed record TBankTinkoffPayStatusResponse : TBankResponse
{
    /// <summary>Параметры ответа.</summary>
    public TBankTinkoffPayStatusParams? Params { get; init; }
}

/// <summary>
/// Параметры ответа проверки доступности T‑Pay.
/// </summary>
public sealed record TBankTinkoffPayStatusParams
{
    /// <summary>Признак доступности проведения платежа T‑Pay на терминале.</summary>
    public bool Allowed { get; init; }

    /// <summary>Версия T‑Pay, доступная на терминале.</summary>
    public string? Version { get; init; }
}

/// <summary>
/// Ответ метода получения ссылки T‑Pay.
/// </summary>
public sealed record TBankTinkoffPayLinkResponse : TBankResponse
{
    /// <summary>Параметры ответа.</summary>
    public TBankTinkoffPayLinkParams? Params { get; init; }
}

/// <summary>
/// Параметры ссылки T‑Pay.
/// </summary>
public sealed record TBankTinkoffPayLinkParams
{
    /// <summary>Ссылка для безусловного редиректа на устройстве покупателя.</summary>
    public string? RedirectUrl { get; init; }

    /// <summary>Ссылка на веб-версию QR.</summary>
    public string? WebQR { get; init; }
}

/// <summary>
/// Ответ метода получения ссылки SberPay.
/// </summary>
public sealed record TBankSberPayLinkResponse : TBankResponse
{
    /// <summary>Параметры ответа.</summary>
    public TBankSberPayLinkParams? Params { get; init; }
}

/// <summary>
/// Параметры ссылки SberPay.
/// </summary>
public sealed record TBankSberPayLinkParams
{
    /// <summary>Ссылка для редиректа на устройстве покупателя.</summary>
    public string? RedirectUrl { get; init; }
}

/// <summary>
/// Ответ метода MirPay/GetDeepLink.
/// </summary>
public sealed record TBankMirPayDeepLinkResponse : TBankResponse
{
    /// <summary>DeepLink, сформированный и подписанный JWT-токеном.</summary>
    public string? Deeplink { get; init; }
}

/// <summary>
/// Ответ метода AlfaPay/link/get.
/// </summary>
public sealed record TBankAlfaPayLinkResponse : TBankResponse
{
    /// <summary>Параметры ответа.</summary>
    public TBankAlfaPayLinkParams? Params { get; init; }
}

/// <summary>
/// Параметры ссылки Alfa Pay.
/// </summary>
public sealed record TBankAlfaPayLinkParams
{
    /// <summary>Ссылка для редиректа на устройстве покупателя.</summary>
    public string? RedirectUrl { get; init; }
}

/// <summary>
/// Ответ метода SendClosingReceipt.
/// </summary>
public sealed record TBankSendClosingReceiptResponse : TBankResponse
{
}
