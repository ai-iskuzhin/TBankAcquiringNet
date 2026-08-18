using System.Net.Security;
using TBankAcquiringNet;

namespace TBankAcquiringNet.Tests;

public sealed class TBankHttpClientFactoryTests
{
    [Fact]
    public void CreateHandler_InstallsTheRussianTrustedCaValidator()
    {
        using var handler = TBankHttpClientFactory.CreateHandler();

        var callback = handler.ServerCertificateCustomValidationCallback;

        Assert.NotNull(callback);
        Assert.Same(TBankServerCertificateValidator.RussianTrustedCa, callback!.Target);
        Assert.True(callback(new HttpRequestMessage(), null, null, SslPolicyErrors.None));
    }

    [Fact]
    public void CreateHandler_RejectsAnUntrustedChain()
    {
        using var handler = TBankHttpClientFactory.CreateHandler();

        using var rogueRoot = TestCertificateAuthority.CreateRoot("Rogue Root CA");
        using var leaf = TestCertificateAuthority.CreateLeaf("securepay.tinkoff.ru", rogueRoot, "securepay.tinkoff.ru");
        using var presented = TestCertificateAuthority.BuildPresentedChain(leaf, rogueRoot);

        Assert.False(handler.ServerCertificateCustomValidationCallback!(
            new HttpRequestMessage(),
            leaf,
            presented,
            SslPolicyErrors.RemoteCertificateChainErrors));
    }

    [Fact]
    public void CreateHttpClient_ReturnsAUsableClient()
    {
        using var httpClient = TBankHttpClientFactory.CreateHttpClient();

        Assert.NotNull(httpClient);
    }
}
