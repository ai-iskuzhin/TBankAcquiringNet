using System.Net;

namespace TBankAcquiringNet.Payments;

/// <summary>
/// Метаданные HTTP-ответа T-Bank для диагностики и трассировки.
/// </summary>
public sealed class TBankAcquiringResponseMetadata
{
    /// <summary>
    /// Создает метаданные ответа.
    /// </summary>
    public TBankAcquiringResponseMetadata(
        HttpStatusCode httpStatusCode,
        IReadOnlyDictionary<string, string[]> headers,
        string? rawResponseBody)
    {
        HttpStatusCode = httpStatusCode;
        Headers = headers;
        RawResponseBody = rawResponseBody;
    }

    /// <summary>HTTP-статус ответа.</summary>
    public HttpStatusCode HttpStatusCode { get; }

    /// <summary>Заголовки ответа и тела ответа.</summary>
    public IReadOnlyDictionary<string, string[]> Headers { get; }

    /// <summary>Сырое тело ответа, если включен CaptureRawResponseBody.</summary>
    public string? RawResponseBody { get; }
}
