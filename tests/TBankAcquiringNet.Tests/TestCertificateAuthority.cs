using System.Security.Cryptography;
using System.Security.Cryptography.X509Certificates;

namespace TBankAcquiringNet.Tests;

/// <summary>
/// Builds throwaway CA hierarchies so certificate validation can be tested without network access.
/// </summary>
internal static class TestCertificateAuthority
{
    public static X509Certificate2 CreateRoot(string commonName)
    {
        using var key = RSA.Create(2048);

        var request = new CertificateRequest(
            $"CN={commonName}",
            key,
            HashAlgorithmName.SHA256,
            RSASignaturePadding.Pkcs1);

        request.CertificateExtensions.Add(new X509BasicConstraintsExtension(true, false, 0, true));
        request.CertificateExtensions.Add(
            new X509KeyUsageExtension(X509KeyUsageFlags.KeyCertSign | X509KeyUsageFlags.CrlSign, true));
        request.CertificateExtensions.Add(new X509SubjectKeyIdentifierExtension(request.PublicKey, false));

        return request.CreateSelfSigned(
            DateTimeOffset.UtcNow.AddYears(-20),
            DateTimeOffset.UtcNow.AddYears(20));
    }

    // Each tier gets a strictly wider window than the one below it, so the seconds that elapse
    // between creating an issuer and its subject can never push the subject outside issuer validity.
    public static X509Certificate2 CreateIntermediate(string commonName, X509Certificate2 issuer) =>
        CreateSigned(
            commonName,
            issuer,
            isCertificateAuthority: true,
            dnsName: null,
            notBefore: DateTimeOffset.UtcNow.AddYears(-5),
            notAfter: DateTimeOffset.UtcNow.AddYears(5));

    public static X509Certificate2 CreateLeaf(
        string commonName,
        X509Certificate2 issuer,
        string dnsName,
        DateTimeOffset? notBefore = null,
        DateTimeOffset? notAfter = null) =>
        CreateSigned(commonName, issuer, isCertificateAuthority: false, dnsName, notBefore, notAfter);

    private static X509Certificate2 CreateSigned(
        string commonName,
        X509Certificate2 issuer,
        bool isCertificateAuthority,
        string? dnsName,
        DateTimeOffset? notBefore = null,
        DateTimeOffset? notAfter = null)
    {
        using var key = RSA.Create(2048);

        var request = new CertificateRequest(
            $"CN={commonName}",
            key,
            HashAlgorithmName.SHA256,
            RSASignaturePadding.Pkcs1);

        request.CertificateExtensions.Add(
            new X509BasicConstraintsExtension(isCertificateAuthority, false, 0, true));
        request.CertificateExtensions.Add(new X509SubjectKeyIdentifierExtension(request.PublicKey, false));

        if (dnsName is not null)
        {
            var alternativeNames = new SubjectAlternativeNameBuilder();
            alternativeNames.AddDnsName(dnsName);
            request.CertificateExtensions.Add(alternativeNames.Build());
        }

        var serialNumber = new byte[8];
        RandomNumberGenerator.Fill(serialNumber);
        serialNumber[0] |= 0x01;

        using var signed = request.Create(
            issuer,
            notBefore ?? DateTimeOffset.UtcNow.AddYears(-1),
            notAfter ?? DateTimeOffset.UtcNow.AddYears(1),
            serialNumber);

        return signed.CopyWithPrivateKey(key);
    }

    /// <summary>
    /// Approximates the chain the TLS stack hands to the validation callback: the leaf plus any
    /// intermediates the server sent, with no trusted root attached.
    /// </summary>
    public static X509Chain BuildPresentedChain(X509Certificate2 leaf, params X509Certificate2[] intermediates)
    {
        var chain = new X509Chain();
        chain.ChainPolicy.RevocationMode = X509RevocationMode.NoCheck;
        chain.ChainPolicy.VerificationFlags = X509VerificationFlags.AllowUnknownCertificateAuthority;
        chain.ChainPolicy.ExtraStore.AddRange(intermediates);
        chain.Build(leaf);
        return chain;
    }
}
