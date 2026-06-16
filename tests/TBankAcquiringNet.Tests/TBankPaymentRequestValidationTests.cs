using TBankAcquiringNet;

namespace TBankAcquiringNet.Tests;

public sealed class TBankPaymentRequestValidationTests
{
    [Fact]
    public async Task InitAsync_RequiresMinimumAmount()
    {
        using var handler = new CountingHandler();
        using var httpClient = new HttpClient(handler);
        var client = CreateClient(httpClient);

        var exception = await Assert.ThrowsAsync<TBankAcquiringValidationException>(() => client.InitAsync(new TBankInitPaymentRequest
        {
            Amount = TBankAmount.FromMinorUnits(99),
            OrderId = "order-1"
        }));

        Assert.Contains("Amount", exception.Message);
        Assert.Equal(0, handler.SendCount);
    }

    [Fact]
    public async Task InitAsync_RequiresOrderId()
    {
        using var handler = new CountingHandler();
        using var httpClient = new HttpClient(handler);
        var client = CreateClient(httpClient);

        var exception = await Assert.ThrowsAsync<TBankAcquiringValidationException>(() => client.InitAsync(new TBankInitPaymentRequest
        {
            Amount = TBankAmount.FromMinorUnits(100),
            OrderId = " "
        }));

        Assert.Contains("OrderId", exception.Message);
        Assert.Equal(0, handler.SendCount);
    }

    [Fact]
    public async Task InitAsync_LimitsDataPairs()
    {
        using var handler = new CountingHandler();
        using var httpClient = new HttpClient(handler);
        var client = CreateClient(httpClient);
        var data = Enumerable.Range(1, 21).ToDictionary(index => $"key{index}", index => (string?)$"value{index}");

        var exception = await Assert.ThrowsAsync<TBankAcquiringValidationException>(() => client.InitAsync(new TBankInitPaymentRequest
        {
            Amount = TBankAmount.FromMinorUnits(100),
            OrderId = "order-1",
            DATA = data
        }));

        Assert.Contains("DATA", exception.Message);
        Assert.Equal(0, handler.SendCount);
    }

    [Theory]
    [InlineData("GetState")]
    [InlineData("CheckOrder")]
    [InlineData("Cancel")]
    [InlineData("Confirm")]
    [InlineData("GetQr")]
    public async Task PaymentIdMethods_RequirePaymentId(string method)
    {
        using var handler = new CountingHandler();
        using var httpClient = new HttpClient(handler);
        var client = CreateClient(httpClient);

        var exception = await Assert.ThrowsAsync<TBankAcquiringValidationException>(() => method switch
        {
            "GetState" => client.GetStateAsync(new TBankPaymentStateRequest { PaymentId = " " }),
            "CheckOrder" => client.CheckOrderAsync(new TBankCheckOrderRequest { OrderId = " " }),
            "Cancel" => client.CancelAsync(new TBankCancelPaymentRequest { PaymentId = " " }),
            "Confirm" => client.ConfirmAsync(new TBankConfirmPaymentRequest { PaymentId = " " }),
            "GetQr" => client.GetQrAsync(new TBankQrRequest { PaymentId = " " }),
            _ => throw new InvalidOperationException()
        });

        Assert.Contains(method == "CheckOrder" ? "OrderId" : "PaymentId", exception.Message);
        Assert.Equal(0, handler.SendCount);
    }

    [Fact]
    public async Task CancelAsync_RequiresMinimumAmountWhenAmountIsProvided()
    {
        using var handler = new CountingHandler();
        using var httpClient = new HttpClient(handler);
        var client = CreateClient(httpClient);

        var exception = await Assert.ThrowsAsync<TBankAcquiringValidationException>(() => client.CancelAsync(new TBankCancelPaymentRequest
        {
            PaymentId = "20150",
            Amount = TBankAmount.FromMinorUnits(99)
        }));

        Assert.Contains("Amount", exception.Message);
        Assert.Equal(0, handler.SendCount);
    }

    [Fact]
    public async Task ChargeQrAsync_RequiresAccountToken()
    {
        using var handler = new CountingHandler();
        using var httpClient = new HttpClient(handler);
        var client = CreateClient(httpClient);

        var exception = await Assert.ThrowsAsync<TBankAcquiringValidationException>(() => client.ChargeQrAsync(new TBankChargeQrRequest
        {
            PaymentId = "20150",
            AccountToken = " "
        }));

        Assert.Contains("AccountToken", exception.Message);
        Assert.Equal(0, handler.SendCount);
    }

    [Fact]
    public async Task ChargeQrAsync_RequiresInfoEmailWhenSendEmailIsTrue()
    {
        using var handler = new CountingHandler();
        using var httpClient = new HttpClient(handler);
        var client = CreateClient(httpClient);

        var exception = await Assert.ThrowsAsync<TBankAcquiringValidationException>(() => client.ChargeQrAsync(new TBankChargeQrRequest
        {
            PaymentId = "20150",
            AccountToken = "account-token",
            SendEmail = true
        }));

        Assert.Contains("InfoEmail", exception.Message);
        Assert.Equal(0, handler.SendCount);
    }

    private static TBankPaymentsClient CreateClient(HttpClient httpClient)
    {
        return new TBankPaymentsClient(httpClient, new TBankPaymentsClientOptions
        {
            TerminalKey = "TerminalKey",
            Password = "Password",
            BaseAddress = new Uri("https://example.test/v2/")
        });
    }

    private sealed class CountingHandler : HttpMessageHandler
    {
        public int SendCount { get; private set; }

        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            SendCount++;
            return Task.FromResult(new HttpResponseMessage(System.Net.HttpStatusCode.OK));
        }
    }
}
