using System.Net;

namespace TBankAcquiringNet.Multisplit.Shops;

/// <summary>
/// Базовое исключение SDK для регистрации точек T-Bank Multisplit.
/// </summary>
public abstract class TBankMultisplitShopsException : Exception
{
    /// <summary>Создает исключение SDK.</summary>
    protected TBankMultisplitShopsException(string message)
        : base(message)
    {
    }

    /// <summary>Создает исключение SDK с внутренней причиной.</summary>
    protected TBankMultisplitShopsException(string message, Exception? innerException)
        : base(message, innerException)
    {
    }
}

/// <summary>
/// Ошибка транспорта: запрос не получил корректный HTTP-ответ.
/// </summary>
public sealed class TBankMultisplitShopsTransportException : TBankMultisplitShopsException
{
    /// <summary>Создает исключение транспортного уровня.</summary>
    public TBankMultisplitShopsTransportException(string message, Exception innerException)
        : base(message, innerException)
    {
    }
}

/// <summary>
/// Ошибка протокола: ответ получен, но его невозможно безопасно разобрать.
/// </summary>
public sealed class TBankMultisplitShopsProtocolException : TBankMultisplitShopsException
{
    /// <summary>Создает исключение протокола T-Bank.</summary>
    public TBankMultisplitShopsProtocolException(
        string message,
        HttpStatusCode? httpStatusCode = null,
        string? responseBodyPreview = null,
        Exception? innerException = null)
        : base(message, innerException)
    {
        HttpStatusCode = httpStatusCode;
        ResponseBodyPreview = responseBodyPreview;
    }

    /// <summary>HTTP-статус ответа, если он был получен.</summary>
    public HttpStatusCode? HttpStatusCode { get; }

    /// <summary>Короткий отредактированный фрагмент тела ответа для диагностики.</summary>
    public string? ResponseBodyPreview { get; }
}

/// <summary>
/// Ошибка локальной валидации запроса до отправки в T-Bank.
/// </summary>
public sealed class TBankMultisplitShopsValidationException : TBankMultisplitShopsException
{
    /// <summary>Создает исключение локальной валидации.</summary>
    public TBankMultisplitShopsValidationException(string message)
        : base(message)
    {
    }
}

/// <summary>
/// Ошибка API регистрации точек T-Bank Multisplit.
/// </summary>
public sealed class TBankMultisplitShopsApiException : TBankMultisplitShopsException
{
    /// <summary>Создает исключение API T-Bank.</summary>
    public TBankMultisplitShopsApiException(
        string message,
        HttpStatusCode httpStatusCode,
        TBankMultisplitShopsErrorResponse? errorResponse,
        TBankMultisplitShopsResponseMetadata metadata)
        : base(message)
    {
        HttpStatusCode = httpStatusCode;
        ErrorResponse = errorResponse;
        Metadata = metadata;
    }

    /// <summary>HTTP-статус ответа.</summary>
    public HttpStatusCode HttpStatusCode { get; }

    /// <summary>Типизированное тело ошибки, если его удалось разобрать.</summary>
    public TBankMultisplitShopsErrorResponse? ErrorResponse { get; }

    /// <summary>HTTP-метаданные ответа.</summary>
    public TBankMultisplitShopsResponseMetadata Metadata { get; }
}
