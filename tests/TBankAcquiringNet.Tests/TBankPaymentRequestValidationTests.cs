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

    [Fact]
    public async Task Check3dsVersionAsync_RequiresCardData()
    {
        using var handler = new CountingHandler();
        using var httpClient = new HttpClient(handler);
        var client = CreateClient(httpClient);

        var exception = await Assert.ThrowsAsync<TBankAcquiringValidationException>(() => client.Check3dsVersionAsync(new TBankCheck3dsVersionRequest
        {
            PaymentId = "13660",
            CardData = " "
        }));

        Assert.Contains("CardData", exception.Message);
        Assert.Equal(0, handler.SendCount);
    }

    [Fact]
    public async Task AttachCardAsync_RequiresRequestKey()
    {
        using var handler = new CountingHandler();
        using var httpClient = new HttpClient(handler);
        var client = CreateClient(httpClient);

        var exception = await Assert.ThrowsAsync<TBankAcquiringValidationException>(() => client.AttachCardAsync(new TBankAttachCardRequest
        {
            RequestKey = " ",
            CardData = "encrypted-card"
        }));

        Assert.Contains("RequestKey", exception.Message);
        Assert.Equal(0, handler.SendCount);
    }

    [Fact]
    public async Task Submit3DSAuthorizationAsync_RequiresPaRes()
    {
        using var handler = new CountingHandler();
        using var httpClient = new HttpClient(handler);
        var client = CreateClient(httpClient);

        var exception = await Assert.ThrowsAsync<TBankAcquiringValidationException>(() => client.Submit3DSAuthorizationAsync(new TBankSubmit3DSAuthorizationRequest
        {
            MD = "md-value",
            PaRes = " "
        }));

        Assert.Contains("PaRes", exception.Message);
        Assert.Equal(0, handler.SendCount);
    }

    [Fact]
    public async Task Submit3DSAuthorizationV2Async_RequiresPaymentId()
    {
        using var handler = new CountingHandler();
        using var httpClient = new HttpClient(handler);
        var client = CreateClient(httpClient);

        var exception = await Assert.ThrowsAsync<TBankAcquiringValidationException>(() => client.Submit3DSAuthorizationV2Async(new TBankSubmit3DSAuthorizationV2Request
        {
            PaymentId = " "
        }));

        Assert.Contains("PaymentId", exception.Message);
        Assert.Equal(0, handler.SendCount);
    }

    [Fact]
    public async Task GetConfirmOperationAsync_RequiresNonEmptyPaymentIdList()
    {
        using var handler = new CountingHandler();
        using var httpClient = new HttpClient(handler);
        var client = CreateClient(httpClient);

        var exception = await Assert.ThrowsAsync<TBankAcquiringValidationException>(() => client.GetConfirmOperationAsync(new TBankGetConfirmOperationRequest
        {
            CallbackUrl = "https://merchant.test/confirm",
            PaymentIdList = []
        }));

        Assert.Contains("PaymentIdList", exception.Message);
        Assert.Equal(0, handler.SendCount);
    }

    [Fact]
    public async Task GetConfirmOperationAsync_RequiresExactlyOneDeliveryChannel()
    {
        using var handler = new CountingHandler();
        using var httpClient = new HttpClient(handler);
        var client = CreateClient(httpClient);

        // Neither channel provided.
        var neither = await Assert.ThrowsAsync<TBankAcquiringValidationException>(() => client.GetConfirmOperationAsync(new TBankGetConfirmOperationRequest
        {
            PaymentIdList = [13660]
        }));
        Assert.Contains("CallbackUrl", neither.Message);
        Assert.Contains("EmailList", neither.Message);

        // Both channels provided.
        await Assert.ThrowsAsync<TBankAcquiringValidationException>(() => client.GetConfirmOperationAsync(new TBankGetConfirmOperationRequest
        {
            CallbackUrl = "https://merchant.test/confirm",
            EmailList = [new TBankConfirmOperationEmail { Email = "ops@merchant.test" }],
            PaymentIdList = [13660]
        }));

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
