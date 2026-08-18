namespace TBankAcquiringNet;

/// <summary>
/// Фабрика транспорта для T-API: <see cref="HttpClientHandler"/> и <see cref="HttpClient"/>,
/// доверяющие корням Минцифры России в дополнение к системному хранилищу.
/// </summary>
/// <remarks>
/// TLS-сертификат T-API выпущен Национальным удостоверяющим центром Минцифры России
/// (Russian Trusted CA), корень которого отсутствует в хранилищах большинства ОС и рантаймов.
/// Клиент <see cref="TBankPaymentsClient"/> принимает <see cref="HttpClient"/> извне и не настраивает
/// транспорт сам, поэтому доверие подключается здесь.
/// </remarks>
/// <example>
/// <code>
/// using var httpClient = TBankHttpClientFactory.CreateHttpClient();
/// var client = new TBankPaymentsClient(httpClient, options);
/// </code>
/// </example>
public static class TBankHttpClientFactory
{
    /// <summary>
    /// Создает <see cref="HttpClientHandler"/> с проверкой сертификата через
    /// <see cref="TBankServerCertificateValidator.RussianTrustedCa"/>.
    /// </summary>
    /// <remarks>
    /// Подходит для <c>IHttpClientFactory</c>:
    /// <c>.ConfigurePrimaryHttpMessageHandler(TBankHttpClientFactory.CreateHandler)</c>.
    /// На .NET Framework требуется 4.7.1 или новее: в более ранних версиях
    /// <see cref="HttpClientHandler.ServerCertificateCustomValidationCallback"/> выбрасывает
    /// <see cref="PlatformNotSupportedException"/>.
    /// </remarks>
    public static HttpClientHandler CreateHandler()
    {
        var handler = new HttpClientHandler();

        try
        {
            handler.ServerCertificateCustomValidationCallback = TBankServerCertificateValidator.RussianTrustedCa.Validate;
        }
        catch
        {
            handler.Dispose();
            throw;
        }

        return handler;
    }

    /// <summary>
    /// Создает <see cref="HttpClient"/> поверх <see cref="CreateHandler"/>.
    /// </summary>
    /// <remarks>Вызывающий код владеет клиентом и освобождает его.</remarks>
    public static HttpClient CreateHttpClient()
    {
        var handler = CreateHandler();

        try
        {
            return new HttpClient(handler, disposeHandler: true);
        }
        catch
        {
            handler.Dispose();
            throw;
        }
    }
}
