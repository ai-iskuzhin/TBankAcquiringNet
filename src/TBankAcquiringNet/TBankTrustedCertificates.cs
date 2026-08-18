using System.Reflection;
using System.Security.Cryptography.X509Certificates;

namespace TBankAcquiringNet;

/// <summary>
/// Сертификаты Национального удостоверяющего центра Минцифры России (Russian Trusted CA),
/// которыми подписан TLS-сертификат T-API.
/// </summary>
/// <remarks>
/// Эти корни не входят в доверенные хранилища большинства ОС и рантаймов, поэтому запросы к T-API
/// падают с ошибкой проверки TLS-сертификата (<c>PartialChain</c>, <c>UntrustedRoot</c>,
/// <c>unable to get local issuer certificate</c>). Сертификаты встроены в сборку как ресурсы;
/// подключить их к транспорту проще всего через <see cref="TBankHttpClientFactory"/>.
/// <para>
/// В комплект входит только RSA-цепочка. ГОСТ-сертификаты Минцифры (ГОСТ Р 34.10-2012)
/// намеренно не встроены: .NET не умеет проверять ГОСТ-подписи и не поддерживает ГОСТ-шифронаборы
/// TLS, поэтому доверять им из .NET невозможно — построение цепочки завершается <c>PartialChain</c>.
/// </para>
/// </remarks>
public static class TBankTrustedCertificates
{
    private const string ResourcePrefix = "TBankAcquiringNet.Certificates.";

    private static readonly string[] RootResourceNames =
    {
        ResourcePrefix + "russian-trusted-root-ca.crt"
    };

    private static readonly string[] IntermediateResourceNames =
    {
        ResourcePrefix + "russian-trusted-sub-ca-2024.crt",
        ResourcePrefix + "russian-trusted-sub-ca-2022.crt"
    };

    /// <summary>
    /// Создает коллекцию корневых сертификатов Минцифры (Russian Trusted Root CA).
    /// </summary>
    /// <returns>Новая коллекция; вызывающий код владеет сертификатами и освобождает их.</returns>
    public static X509Certificate2Collection CreateRootCertificates() => Load(RootResourceNames);

    /// <summary>
    /// Создает коллекцию промежуточных сертификатов Минцифры (Russian Trusted Sub CA).
    /// </summary>
    /// <remarks>
    /// Используются только для построения цепочки и не являются якорями доверия. T-API отдает
    /// промежуточный сертификат в TLS-рукопожатии, но встроенная копия страхует серверы,
    /// которые его не присылают.
    /// </remarks>
    /// <returns>Новая коллекция; вызывающий код владеет сертификатами и освобождает их.</returns>
    public static X509Certificate2Collection CreateIntermediateCertificates() => Load(IntermediateResourceNames);

    private static X509Certificate2Collection Load(string[] resourceNames)
    {
        var certificates = new X509Certificate2Collection();

        foreach (var resourceName in resourceNames)
        {
            certificates.Add(LoadCertificate(resourceName));
        }

        return certificates;
    }

    private static X509Certificate2 LoadCertificate(string resourceName)
    {
        var assembly = typeof(TBankTrustedCertificates).GetTypeInfo().Assembly;

        using var stream = assembly.GetManifestResourceStream(resourceName)
            ?? throw new InvalidOperationException(
                $"Embedded certificate '{resourceName}' was not found in {assembly.FullName}.");

        using var buffer = new MemoryStream();
        stream.CopyTo(buffer);

        // The PEM is decoded by hand because X509Certificate2.CreateFromPem is not available on
        // netstandard2.0, and the byte[] constructor is obsolete from net9.0 onwards.
        var der = DecodePem(buffer.ToArray(), resourceName);

#if NET9_0_OR_GREATER
        return X509CertificateLoader.LoadCertificate(der);
#else
        return new X509Certificate2(der);
#endif
    }

    private static byte[] DecodePem(byte[] pem, string resourceName)
    {
        const string Header = "-----BEGIN CERTIFICATE-----";
        const string Footer = "-----END CERTIFICATE-----";

        var text = System.Text.Encoding.ASCII.GetString(pem);

        var start = text.IndexOf(Header, StringComparison.Ordinal);
        var end = text.IndexOf(Footer, StringComparison.Ordinal);

        if (start < 0 || end < 0 || end <= start)
        {
            throw new InvalidOperationException(
                $"Embedded certificate '{resourceName}' is not a PEM-encoded certificate.");
        }

        start += Header.Length;

        var base64 = text
            .Substring(start, end - start)
            .Replace("\r", string.Empty)
            .Replace("\n", string.Empty);

        return Convert.FromBase64String(base64);
    }
}
