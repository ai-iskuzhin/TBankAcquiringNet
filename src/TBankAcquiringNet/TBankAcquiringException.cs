using System.Net;

namespace TBankAcquiringNet;

/// <summary>
/// Базовое исключение SDK TBankAcquiringNet.
/// </summary>
/// <remarks>
/// Исключения SDK используются для транспортных, протокольных и локальных ошибок.
/// Бизнес-ошибки T-Bank с Success=false по умолчанию возвращаются в типизированном ответе.
/// </remarks>
public abstract class TBankAcquiringException : Exception
{
    /// <summary>
    /// Создает исключение SDK.
    /// </summary>
    /// <param name="message">Сообщение исключения.</param>
    protected TBankAcquiringException(string message)
        : base(message)
    {
    }

    /// <summary>
    /// Создает исключение SDK с внутренней причиной.
    /// </summary>
    /// <param name="message">Сообщение исключения.</param>
    /// <param name="innerException">Внутренняя причина.</param>
    protected TBankAcquiringException(string message, Exception? innerException)
        : base(message, innerException)
    {
    }
}

/// <summary>
/// Ошибка транспорта: запрос не получил корректный HTTP-ответ.
/// </summary>
/// <remarks>Например DNS, TLS, сетевой сбой или ошибка HttpClient до получения ответа.</remarks>
public sealed class TBankAcquiringTransportException : TBankAcquiringException
{
    /// <summary>
    /// Создает исключение транспортного уровня.
    /// </summary>
    /// <param name="message">Сообщение исключения.</param>
    /// <param name="innerException">Исходная ошибка транспорта.</param>
    public TBankAcquiringTransportException(string message, Exception innerException)
        : base(message, innerException)
    {
    }
}

/// <summary>
/// Ошибка протокола: ответ получен, но его невозможно безопасно разобрать как ответ T-Bank.
/// </summary>
/// <remarks>Например HTML-страница 403, пустое тело или JSON неожиданной формы.</remarks>
public sealed class TBankAcquiringProtocolException : TBankAcquiringException
{
    /// <summary>
    /// Создает исключение протокола T-Bank.
    /// </summary>
    /// <param name="message">Сообщение исключения.</param>
    /// <param name="httpStatusCode">HTTP-статус ответа.</param>
    /// <param name="responseBodyPreview">Короткий отредактированный фрагмент тела ответа.</param>
    /// <param name="innerException">Исходная ошибка разбора ответа.</param>
    public TBankAcquiringProtocolException(
        string message,
        HttpStatusCode? httpStatusCode = null,
        string? responseBodyPreview = null,
        Exception? innerException = null)
        : base(message, innerException)
    {
        HttpStatusCode = httpStatusCode;
        ResponseBodyPreview = responseBodyPreview;
    }

    /// <summary>
    /// HTTP-статус ответа, если он был получен.
    /// </summary>
    public HttpStatusCode? HttpStatusCode { get; }

    /// <summary>
    /// Короткий отредактированный фрагмент тела ответа для диагностики.
    /// </summary>
    public string? ResponseBodyPreview { get; }
}

/// <summary>
/// Ошибка локальной валидации запроса до отправки в T-Bank.
/// </summary>
/// <remarks>Используется для очевидно некорректных запросов: пустой PaymentId, слишком маленькая сумма и т.п.</remarks>
public sealed class TBankAcquiringValidationException : TBankAcquiringException
{
    /// <summary>
    /// Создает исключение локальной валидации.
    /// </summary>
    /// <param name="message">Описание ошибки валидации.</param>
    public TBankAcquiringValidationException(string message)
        : base(message)
    {
    }
}

/// <summary>
/// Ошибка API T-Bank при включенном строгом режиме ThrowOnTBankApiError.
/// </summary>
/// <remarks>
/// Возникает только если <see cref="TBankPaymentsClientOptions.ThrowOnTBankApiError"/> включен.
/// Иначе ответы T-Bank с Success=false возвращаются как обычные response-модели.
/// </remarks>
public sealed class TBankAcquiringApiException : TBankAcquiringException
{
    /// <summary>
    /// Создает исключение API T-Bank для строгого режима.
    /// </summary>
    /// <param name="message">Сообщение исключения.</param>
    /// <param name="errorCode">Код ошибки T-Bank.</param>
    /// <param name="errorMessage">Сообщение ошибки T-Bank.</param>
    /// <param name="details">Детали ошибки T-Bank.</param>
    /// <param name="httpStatusCode">HTTP-статус ответа.</param>
    public TBankAcquiringApiException(
        string message,
        string? errorCode,
        string? errorMessage,
        string? details,
        HttpStatusCode? httpStatusCode)
        : base(message)
    {
        ErrorCode = errorCode;
        ErrorMessage = errorMessage;
        Details = details;
        HttpStatusCode = httpStatusCode;
    }

    /// <summary>Код ошибки T-Bank.</summary>
    public string? ErrorCode { get; }

    /// <summary>Сообщение ошибки T-Bank.</summary>
    public string? ErrorMessage { get; }

    /// <summary>Детали ошибки T-Bank.</summary>
    public string? Details { get; }

    /// <summary>HTTP-статус ответа, если он был получен.</summary>
    public HttpStatusCode? HttpStatusCode { get; }
}
