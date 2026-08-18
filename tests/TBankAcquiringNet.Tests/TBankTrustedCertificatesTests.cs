using System.Security.Cryptography.X509Certificates;
using TBankAcquiringNet;

namespace TBankAcquiringNet.Tests;

public sealed class TBankTrustedCertificatesTests
{
    // SHA-256 thumbprints published by the Минцифры CA. They pin exactly which certificates ship
    // in the package, so an accidental swap or re-encode fails the build instead of production TLS.
    private const string RootThumbprint =
        "D26D2D0231B7C39F92CC738512BA54103519E4405D68B5BD703E9788CA8ECF31";

    private const string SubCa2024Thumbprint =
        "21557850 36C900DB B5F1BB2A 1569C80C 55595BD6 BF94867A 29BBDDBC 7D88A3F2";

    private const string SubCa2022Thumbprint =
        "BBBDE210 3E790B99 9EC62BD0 3CF625A5 A2E7C316 E10AFE6A 490EEDEA D8B3FD9B";

    [Fact]
    public void CreateRootCertificates_ReturnsTheRussianTrustedRootCa()
    {
        var roots = TBankTrustedCertificates.CreateRootCertificates();

        var root = Assert.Single(roots);

        Assert.Contains("Russian Trusted Root CA", root.Subject);
        Assert.Equal(root.Subject, root.Issuer);
        Assert.Equal(Normalize(RootThumbprint), Thumbprint(root));
    }

    [Fact]
    public void CreateIntermediateCertificates_ReturnsBothRussianTrustedSubCas()
    {
        var intermediates = TBankTrustedCertificates.CreateIntermediateCertificates();

        Assert.Equal(2, intermediates.Count);
        Assert.All(intermediates, certificate => Assert.Contains("Russian Trusted Sub CA", certificate.Subject));

        var thumbprints = intermediates.Cast<X509Certificate2>().Select(Thumbprint).ToArray();

        Assert.Contains(Normalize(SubCa2024Thumbprint), thumbprints);
        Assert.Contains(Normalize(SubCa2022Thumbprint), thumbprints);
    }

    [Fact]
    public void BundledCertificates_AreCertificateAuthoritiesThatAreStillValid()
    {
        var certificates = TBankTrustedCertificates.CreateRootCertificates();
        certificates.AddRange(TBankTrustedCertificates.CreateIntermediateCertificates());

        var now = DateTime.Now;

        foreach (var certificate in certificates.Cast<X509Certificate2>())
        {
            Assert.True(certificate.NotBefore <= now, $"{certificate.Subject} is not yet valid.");
            Assert.True(certificate.NotAfter > now, $"{certificate.Subject} expired on {certificate.NotAfter:O}.");

            var basicConstraints = certificate.Extensions
                .OfType<X509BasicConstraintsExtension>()
                .SingleOrDefault();

            Assert.NotNull(basicConstraints);
            Assert.True(basicConstraints!.CertificateAuthority, $"{certificate.Subject} is not a CA certificate.");
        }
    }

    [Fact]
    public void BundledIntermediates_ChainToTheBundledRoot()
    {
        var roots = TBankTrustedCertificates.CreateRootCertificates();
        var intermediates = TBankTrustedCertificates.CreateIntermediateCertificates();

        foreach (var intermediate in intermediates.Cast<X509Certificate2>())
        {
            using var chain = new X509Chain();
            chain.ChainPolicy.TrustMode = X509ChainTrustMode.CustomRootTrust;
            chain.ChainPolicy.CustomTrustStore.AddRange(roots);
            chain.ChainPolicy.RevocationMode = X509RevocationMode.NoCheck;

            Assert.True(
                chain.Build(intermediate),
                $"{intermediate.Subject} did not chain to the bundled root: " +
                string.Join(", ", chain.ChainStatus.Select(status => status.Status)));
        }
    }

    [Fact]
    public void CreateRootCertificates_ReturnsFreshInstancesPerCall()
    {
        var first = TBankTrustedCertificates.CreateRootCertificates();
        var second = TBankTrustedCertificates.CreateRootCertificates();

        Assert.NotSame(first[0], second[0]);
        Assert.Equal(Thumbprint(first[0]), Thumbprint(second[0]));
    }

    private static string Thumbprint(X509Certificate2 certificate) =>
        Convert.ToHexString(certificate.GetCertHash(System.Security.Cryptography.HashAlgorithmName.SHA256));

    private static string Normalize(string thumbprint) => thumbprint.Replace(" ", string.Empty);
}
