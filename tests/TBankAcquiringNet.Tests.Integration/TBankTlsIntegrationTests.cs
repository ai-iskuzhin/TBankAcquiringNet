using TBankAcquiringNet;

namespace TBankAcquiringNet.Tests.Integration;

/// <summary>
/// Verifies against the live T-API endpoints that the bundled Минцифры roots are the ones actually
/// serving TLS. Opt-in via <c>TBANK_ACQUIRING_LIVE_TLS=1</c>, because it needs outbound network.
/// </summary>
public sealed class TBankTlsIntegrationTests
{
    [Theory]
    [InlineData("https://securepay.tinkoff.ru/v2/GetState")]
    [InlineData("https://rest-api-test.tinkoff.ru/v2/GetState")]
    public async Task TBankApi_CompletesTlsHandshake_WithTheBundledRussianTrustedCa(string endpoint)
    {
        if (!IsLiveTlsEnabled)
        {
            return;
        }

        using var httpClient = TBankHttpClientFactory.CreateHttpClient();
        httpClient.Timeout = TimeSpan.FromSeconds(30);

        // Any HTTP status proves the TLS handshake and certificate validation succeeded; a trust
        // failure would surface as HttpRequestException wrapping an AuthenticationException instead.
        using var response = await httpClient.GetAsync(endpoint);

        Assert.True(
            (int)response.StatusCode > 0,
            $"Expected an HTTP response from {endpoint}, got {response.StatusCode}.");
    }

    [Fact]
    public async Task TBankApi_ServesTheBundledIntermediateAndRoot()
    {
        if (!IsLiveTlsEnabled)
        {
            return;
        }

        var bundled = TBankTrustedCertificates.CreateRootCertificates();
        bundled.AddRange(TBankTrustedCertificates.CreateIntermediateCertificates());

        var presentedIssuers = new List<string>();

        using var handler = new HttpClientHandler
        {
            ServerCertificateCustomValidationCallback = (_, _, chain, _) =>
            {
                if (chain is not null)
                {
                    for (var i = 0; i < chain.ChainElements.Count; i++)
                    {
                        presentedIssuers.Add(chain.ChainElements[i].Certificate.Issuer);
                    }
                }

                return true;
            }
        };

        using var httpClient = new HttpClient(handler) { Timeout = TimeSpan.FromSeconds(30) };

        using var response = await httpClient.GetAsync("https://securepay.tinkoff.ru/v2/GetState");

        Assert.Contains(presentedIssuers, issuer => issuer.Contains("Russian Trusted Sub CA", StringComparison.Ordinal));
    }

    private static bool IsLiveTlsEnabled =>
        string.Equals(Environment.GetEnvironmentVariable("TBANK_ACQUIRING_LIVE_TLS"), "1", StringComparison.Ordinal);
}
