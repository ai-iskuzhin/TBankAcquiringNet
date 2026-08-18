using System.Net.Security;
using System.Security.Cryptography.X509Certificates;

namespace TBankAcquiringNet;

/// <summary>
/// Проверка TLS-сертификата сервера, дополняющая системное хранилище доверенными корнями
/// (по умолчанию — корнями Минцифры России).
/// </summary>
/// <remarks>
/// Валидатор ничего не ослабляет. Сначала выполняется штатная проверка платформы; дополнительные
/// корни задействуются, только если платформа отвергла цепочку и единственная претензия —
/// <see cref="SslPolicyErrors.RemoteCertificateChainErrors"/>. Несовпадение имени хоста
/// (<see cref="SslPolicyErrors.RemoteCertificateNameMismatch"/>), отсутствие сертификата,
/// истекший срок действия и любые другие ошибки цепочки по-прежнему отклоняются.
/// </remarks>
/// <example>
/// <code>
/// using var handler = new HttpClientHandler
/// {
///     ServerCertificateCustomValidationCallback = TBankServerCertificateValidator.RussianTrustedCa.Validate
/// };
/// </code>
/// </example>
public sealed class TBankServerCertificateValidator
{
#if NETSTANDARD2_0
    private const bool LegacyChainValidationByDefault = true;
#else
    private const bool LegacyChainValidationByDefault = false;
#endif

    private static readonly Lazy<TBankServerCertificateValidator> RussianTrustedCaValidator =
        new(
            () => new TBankServerCertificateValidator(
                TBankTrustedCertificates.CreateRootCertificates(),
                TBankTrustedCertificates.CreateIntermediateCertificates()),
            isThreadSafe: true);

    private readonly X509Certificate2[] trustedRoots;
    private readonly X509Certificate2[] intermediates;
    private readonly X509RevocationMode revocationMode;
    private readonly bool useLegacyChainValidation;

    /// <summary>
    /// Создает валидатор с заданными якорями доверия.
    /// </summary>
    /// <param name="trustedRoots">
    /// Дополнительные корневые сертификаты. Экземпляры удерживаются валидатором, не освобождайте их.
    /// </param>
    /// <param name="additionalIntermediates">
    /// Промежуточные сертификаты для построения цепочки. Якорями доверия не становятся.
    /// </param>
    /// <param name="revocationMode">
    /// Режим проверки отзыва. По умолчанию <see cref="X509RevocationMode.NoCheck"/> — как у
    /// <see cref="HttpClientHandler"/> с выключенным <c>CheckCertificateRevocationList</c>.
    /// </param>
    public TBankServerCertificateValidator(
        IEnumerable<X509Certificate2> trustedRoots,
        IEnumerable<X509Certificate2>? additionalIntermediates = null,
        X509RevocationMode revocationMode = X509RevocationMode.NoCheck)
        : this(
            ToArray(trustedRoots, nameof(trustedRoots)),
            ToArray(additionalIntermediates),
            revocationMode,
            LegacyChainValidationByDefault)
    {
    }

    /// <summary>
    /// Создает валидатор с заданными якорями доверия.
    /// </summary>
    /// <remarks>
    /// Отдельная перегрузка нужна потому, что на netstandard2.0
    /// <see cref="X509Certificate2Collection"/> не реализует <see cref="IEnumerable{T}"/>.
    /// </remarks>
    /// <param name="trustedRoots">
    /// Дополнительные корневые сертификаты. Экземпляры удерживаются валидатором, не освобождайте их.
    /// </param>
    /// <param name="additionalIntermediates">
    /// Промежуточные сертификаты для построения цепочки. Якорями доверия не становятся.
    /// </param>
    /// <param name="revocationMode">Режим проверки отзыва.</param>
    public TBankServerCertificateValidator(
        X509Certificate2Collection trustedRoots,
        X509Certificate2Collection? additionalIntermediates = null,
        X509RevocationMode revocationMode = X509RevocationMode.NoCheck)
        : this(
            Materialize(trustedRoots, nameof(trustedRoots)),
            additionalIntermediates is null
                ? Array.Empty<X509Certificate2>()
                : Materialize(additionalIntermediates, nameof(additionalIntermediates)),
            revocationMode,
            LegacyChainValidationByDefault)
    {
    }

    private TBankServerCertificateValidator(
        X509Certificate2[] trustedRoots,
        X509Certificate2[] intermediates,
        X509RevocationMode revocationMode,
        bool useLegacyChainValidation)
    {
        if (trustedRoots.Length == 0)
        {
            throw new ArgumentException("At least one trusted root certificate is required.", nameof(trustedRoots));
        }

        this.trustedRoots = trustedRoots;
        this.intermediates = intermediates;
        this.revocationMode = revocationMode;
        this.useLegacyChainValidation = useLegacyChainValidation;
    }

    /// <summary>
    /// Валидатор, доверяющий системному хранилищу и корням Минцифры России.
    /// </summary>
    public static TBankServerCertificateValidator RussianTrustedCa => RussianTrustedCaValidator.Value;

    /// <summary>
    /// Проверяет сертификат сервера. Сигнатура совместима с
    /// <see cref="HttpClientHandler.ServerCertificateCustomValidationCallback"/>.
    /// </summary>
    /// <returns><c>true</c>, если сертификат принят.</returns>
    public bool Validate(
        HttpRequestMessage? request,
        X509Certificate2? certificate,
        X509Chain? chain,
        SslPolicyErrors sslPolicyErrors)
    {
        if (sslPolicyErrors == SslPolicyErrors.None)
        {
            return true;
        }

        // Only an untrusted or incomplete chain may be re-examined. A hostname mismatch or a missing
        // certificate is never recoverable by adding trust anchors.
        if (sslPolicyErrors != SslPolicyErrors.RemoteCertificateChainErrors || certificate is null)
        {
            return false;
        }

        using var customChain = new X509Chain();

        customChain.ChainPolicy.RevocationMode = revocationMode;
        customChain.ChainPolicy.ExtraStore.AddRange(intermediates);

        // Intermediates sent during the handshake are not visible to a freshly created chain.
        if (chain is not null)
        {
            for (var i = 0; i < chain.ChainElements.Count; i++)
            {
                customChain.ChainPolicy.ExtraStore.Add(chain.ChainElements[i].Certificate);
            }
        }

#if !NETSTANDARD2_0
        if (!useLegacyChainValidation)
        {
            // CustomRootTrust replaces the system anchors for this chain, so Build succeeds only when
            // the chain terminates in one of our roots. Everything else is validated as usual.
            customChain.ChainPolicy.TrustMode = X509ChainTrustMode.CustomRootTrust;
            customChain.ChainPolicy.CustomTrustStore.AddRange(trustedRoots);

            return customChain.Build(certificate);
        }
#endif

        return BuildWithoutCustomTrustStore(customChain, certificate);
    }

    /// <summary>
    /// Fallback for netstandard2.0, where <c>X509ChainTrustMode.CustomRootTrust</c> does not exist:
    /// build the chain with an unknown authority tolerated, then require that the root it terminates
    /// in is byte-for-byte one of our anchors.
    /// </summary>
    private bool BuildWithoutCustomTrustStore(X509Chain customChain, X509Certificate2 certificate)
    {
        customChain.ChainPolicy.VerificationFlags = X509VerificationFlags.AllowUnknownCertificateAuthority;
        customChain.ChainPolicy.ExtraStore.AddRange(trustedRoots);

        if (!customChain.Build(certificate))
        {
            return false;
        }

        const X509ChainStatusFlags Tolerated = X509ChainStatusFlags.NoError | X509ChainStatusFlags.UntrustedRoot;

        foreach (var status in customChain.ChainStatus)
        {
            if ((status.Status & ~Tolerated) != X509ChainStatusFlags.NoError)
            {
                return false;
            }
        }

        var elements = customChain.ChainElements;

        if (elements.Count == 0)
        {
            return false;
        }

        var root = elements[elements.Count - 1].Certificate;

        return Array.Exists(trustedRoots, anchor => AreSame(anchor, root));
    }

    internal static TBankServerCertificateValidator CreateWithoutCustomTrustStore(
        IEnumerable<X509Certificate2> trustedRoots,
        IEnumerable<X509Certificate2>? additionalIntermediates = null) =>
        new(
            ToArray(trustedRoots, nameof(trustedRoots)),
            ToArray(additionalIntermediates),
            X509RevocationMode.NoCheck,
            useLegacyChainValidation: true);

    private static bool AreSame(X509Certificate2 left, X509Certificate2 right)
    {
        var a = left.RawData;
        var b = right.RawData;

        if (a.Length != b.Length)
        {
            return false;
        }

        var difference = 0;

        for (var i = 0; i < a.Length; i++)
        {
            difference |= a[i] ^ b[i];
        }

        return difference == 0;
    }

    private static X509Certificate2[] ToArray(IEnumerable<X509Certificate2>? certificates, string? paramName = null)
    {
        if (certificates is null)
        {
            return paramName is null
                ? Array.Empty<X509Certificate2>()
                : throw new System.ArgumentNullException(paramName);
        }

        return certificates.ToArray();
    }

    private static X509Certificate2[] Materialize(X509Certificate2Collection certificates, string paramName)
    {
        ArgumentNullException.ThrowIfNull(certificates, paramName);

        var result = new X509Certificate2[certificates.Count];

        for (var i = 0; i < certificates.Count; i++)
        {
            result[i] = certificates[i];
        }

        return result;
    }
}
