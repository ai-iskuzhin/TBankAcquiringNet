using TBankAcquiringNet;

namespace TBankAcquiringNet.Tests.Integration;

public sealed class TBankPaymentsIntegrationTests
{
    [Fact]
    public async Task InitCheckOrderGetStateAndCancelAsync_CanCallTBankTestEnvironment_WhenCredentialsAreConfigured()
    {
        var terminalKey = Environment.GetEnvironmentVariable("TBANK_ACQUIRING_TEST_TERMINAL_KEY");
        var password = Environment.GetEnvironmentVariable("TBANK_ACQUIRING_TEST_PASSWORD");
        var baseAddress = Environment.GetEnvironmentVariable("TBANK_ACQUIRING_TEST_BASE_URL");

        if (string.IsNullOrWhiteSpace(terminalKey) || string.IsNullOrWhiteSpace(password))
        {
            return;
        }

        using var httpClient = TBankHttpClientFactory.CreateHttpClient();
        var client = new TBankPaymentsClient(httpClient, new TBankPaymentsClientOptions
        {
            TerminalKey = terminalKey,
            Password = password,
            Environment = TBankAcquiringEnvironment.Test,
            BaseAddress = string.IsNullOrWhiteSpace(baseAddress) ? null : new Uri(baseAddress)
        });

        var orderId = $"sdk-test-{DateTimeOffset.UtcNow:yyyyMMddHHmmssfff}";
        TBankInitPaymentResponse initResponse;
        try
        {
            initResponse = await client.InitAsync(new TBankInitPaymentRequest
            {
                Amount = TBankAmount.FromMinorUnits(1000),
                OrderId = orderId,
                Description = "TBankAcquiringNet integration test"
            });
        }
        catch (TBankAcquiringProtocolException exception)
        {
            Assert.Fail(
                "T-Bank returned a non-JSON or malformed response for Init. " +
                "This usually means the test terminal/environment is not available for the current IP, credentials, or endpoint. " +
                $"HTTP: {exception.HttpStatusCode}. Preview: {exception.ResponseBodyPreview}");
            throw;
        }

        Assert.True(initResponse.Success, initResponse.Message ?? initResponse.Details);
        Assert.False(string.IsNullOrWhiteSpace(initResponse.PaymentId));

        var orderResponse = await client.CheckOrderAsync(new TBankCheckOrderRequest
        {
            OrderId = orderId
        });

        Assert.True(orderResponse.Success, orderResponse.Message ?? orderResponse.Details);
        Assert.Contains(orderResponse.Payments, payment => payment.PaymentId == initResponse.PaymentId);

        var stateResponse = await client.GetStateAsync(new TBankPaymentStateRequest
        {
            PaymentId = initResponse.PaymentId
        });

        Assert.False(string.IsNullOrWhiteSpace(stateResponse.ErrorCode));
        Assert.Equal(initResponse.PaymentId, stateResponse.PaymentId);

        var cancelResponse = await client.CancelAsync(new TBankCancelPaymentRequest
        {
            PaymentId = initResponse.PaymentId
        });

        Assert.True(cancelResponse.Success, cancelResponse.Message ?? cancelResponse.Details);
        Assert.Equal(initResponse.PaymentId, cancelResponse.PaymentId);

        var canceledStateResponse = await client.GetStateAsync(new TBankPaymentStateRequest
        {
            PaymentId = initResponse.PaymentId
        });

        Assert.False(string.IsNullOrWhiteSpace(canceledStateResponse.ErrorCode));
        Assert.Equal(initResponse.PaymentId, canceledStateResponse.PaymentId);
    }
}
