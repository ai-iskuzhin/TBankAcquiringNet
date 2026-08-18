using System.Net.Security;
using System.Security.Cryptography.X509Certificates;
using TBankAcquiringNet;

namespace TBankAcquiringNet.Tests;

/// <summary>
/// Every chain-building case runs twice: once through <c>X509ChainTrustMode.CustomRootTrust</c>
/// (net8.0+) and once through the netstandard2.0 fallback, so both paths are covered on one runtime.
/// </summary>
public sealed class TBankServerCertificateValidatorTests
{
    [Fact]
    public void Validate_AcceptsCertificateAcceptedByTheSystemTrustStore()
    {
        using var root = TestCertificateAuthority.CreateRoot("Unrelated Test Root");
        var validator = new TBankServerCertificateValidator(new[] { root });

        Assert.True(validator.Validate(null, null, null, SslPolicyErrors.None));
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void Validate_AcceptsLeafChainedToConfiguredRoot(bool withoutCustomTrustStore)
    {
        using var root = TestCertificateAuthority.CreateRoot("Test Root CA");
        using var intermediate = TestCertificateAuthority.CreateIntermediate("Test Sub CA", root);
        using var leaf = TestCertificateAuthority.CreateLeaf("api.test", intermediate, "api.test");

        var validator = Create(withoutCustomTrustStore, new[] { root }, new[] { intermediate });

        using var presented = TestCertificateAuthority.BuildPresentedChain(leaf, intermediate);

        Assert.True(validator.Validate(null, leaf, presented, SslPolicyErrors.RemoteCertificateChainErrors));
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void Validate_AcceptsLeafWhenIntermediateIsOnlyPresentedByTheServer(bool withoutCustomTrustStore)
    {
        using var root = TestCertificateAuthority.CreateRoot("Test Root CA");
        using var intermediate = TestCertificateAuthority.CreateIntermediate("Test Sub CA", root);
        using var leaf = TestCertificateAuthority.CreateLeaf("api.test", intermediate, "api.test");

        var validator = Create(withoutCustomTrustStore, new[] { root });

        using var presented = TestCertificateAuthority.BuildPresentedChain(leaf, intermediate);

        Assert.True(validator.Validate(null, leaf, presented, SslPolicyErrors.RemoteCertificateChainErrors));
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void Validate_RejectsLeafFromAnUnknownRoot(bool withoutCustomTrustStore)
    {
        using var trustedRoot = TestCertificateAuthority.CreateRoot("Test Root CA");
        using var rogueRoot = TestCertificateAuthority.CreateRoot("Rogue Root CA");
        using var rogueIntermediate = TestCertificateAuthority.CreateIntermediate("Rogue Sub CA", rogueRoot);
        using var leaf = TestCertificateAuthority.CreateLeaf("api.test", rogueIntermediate, "api.test");

        var validator = Create(withoutCustomTrustStore, new[] { trustedRoot });

        using var presented = TestCertificateAuthority.BuildPresentedChain(leaf, rogueIntermediate, rogueRoot);

        Assert.False(validator.Validate(null, leaf, presented, SslPolicyErrors.RemoteCertificateChainErrors));
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void Validate_RejectsSelfSignedCertificateImpersonatingTheServer(bool withoutCustomTrustStore)
    {
        using var trustedRoot = TestCertificateAuthority.CreateRoot("Test Root CA");
        using var selfSigned = TestCertificateAuthority.CreateRoot("api.test");

        var validator = Create(withoutCustomTrustStore, new[] { trustedRoot });

        using var presented = TestCertificateAuthority.BuildPresentedChain(selfSigned);

        Assert.False(validator.Validate(null, selfSigned, presented, SslPolicyErrors.RemoteCertificateChainErrors));
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void Validate_RejectsHostNameMismatchEvenForATrustedRoot(bool withoutCustomTrustStore)
    {
        using var root = TestCertificateAuthority.CreateRoot("Test Root CA");
        using var intermediate = TestCertificateAuthority.CreateIntermediate("Test Sub CA", root);
        using var leaf = TestCertificateAuthority.CreateLeaf("other.test", intermediate, "other.test");

        var validator = Create(withoutCustomTrustStore, new[] { root }, new[] { intermediate });

        using var presented = TestCertificateAuthority.BuildPresentedChain(leaf, intermediate);

        Assert.False(validator.Validate(
            null,
            leaf,
            presented,
            SslPolicyErrors.RemoteCertificateNameMismatch));

        Assert.False(validator.Validate(
            null,
            leaf,
            presented,
            SslPolicyErrors.RemoteCertificateChainErrors | SslPolicyErrors.RemoteCertificateNameMismatch));
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void Validate_RejectsExpiredLeafFromATrustedRoot(bool withoutCustomTrustStore)
    {
        using var root = TestCertificateAuthority.CreateRoot("Test Root CA");
        using var intermediate = TestCertificateAuthority.CreateIntermediate("Test Sub CA", root);
        using var leaf = TestCertificateAuthority.CreateLeaf(
            "api.test",
            intermediate,
            "api.test",
            notBefore: DateTimeOffset.UtcNow.AddMonths(-6),
            notAfter: DateTimeOffset.UtcNow.AddDays(-1));

        var validator = Create(withoutCustomTrustStore, new[] { root }, new[] { intermediate });

        using var presented = TestCertificateAuthority.BuildPresentedChain(leaf, intermediate);

        Assert.False(validator.Validate(null, leaf, presented, SslPolicyErrors.RemoteCertificateChainErrors));
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void Validate_RejectsMissingCertificate(bool withoutCustomTrustStore)
    {
        using var root = TestCertificateAuthority.CreateRoot("Test Root CA");
        var validator = Create(withoutCustomTrustStore, new[] { root });

        Assert.False(validator.Validate(null, null, null, SslPolicyErrors.RemoteCertificateNotAvailable));
    }

    [Fact]
    public void Constructor_RejectsEmptyTrustAnchors()
    {
        Assert.Throws<ArgumentException>(() => new TBankServerCertificateValidator(Array.Empty<X509Certificate2>()));
        Assert.Throws<ArgumentNullException>(() => new TBankServerCertificateValidator((X509Certificate2[])null!));
    }

    [Fact]
    public void Constructor_AcceptsACertificateCollection()
    {
        var roots = TBankTrustedCertificates.CreateRootCertificates();
        var intermediates = TBankTrustedCertificates.CreateIntermediateCertificates();

        var validator = new TBankServerCertificateValidator(roots, intermediates);

        Assert.True(validator.Validate(null, null, null, SslPolicyErrors.None));
    }

    [Fact]
    public void RussianTrustedCa_IsCached()
    {
        Assert.Same(TBankServerCertificateValidator.RussianTrustedCa, TBankServerCertificateValidator.RussianTrustedCa);
    }

    private static TBankServerCertificateValidator Create(
        bool withoutCustomTrustStore,
        X509Certificate2[] trustedRoots,
        X509Certificate2[]? intermediates = null) =>
        withoutCustomTrustStore
            ? TBankServerCertificateValidator.CreateWithoutCustomTrustStore(trustedRoots, intermediates)
            : new TBankServerCertificateValidator(trustedRoots, intermediates);
}
