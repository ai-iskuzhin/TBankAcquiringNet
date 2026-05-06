using System.Net;
using System.Text.Json;
using TBankAcquiringNet.Payments;

namespace TBankAcquiringNet.Payments.Tests;

public sealed class TBankPaymentsClientTests
{
    [Fact]
    public async Task InitAsync_PostsSignedRequestToInitEndpoint()
    {
        using var handler = new RecordingHandler("""
            {
              "Success": true,
              "ErrorCode": "0",
              "TerminalKey": "TerminalKey",
              "Status": "NEW",
              "PaymentId": "7277900132",
              "OrderId": "sp123",
              "Amount": "15000",
              "PaymentURL": "https://pay-test.tbank.ru/kwVvZY9L"
            }
            """);
        using var httpClient = new HttpClient(handler);
        var client = new TBankPaymentsClient(httpClient, new TBankPaymentsClientOptions
        {
            TerminalKey = "TerminalKey",
            Password = "Password",
            BaseAddress = new Uri("https://example.test/v2/")
        });

        var response = await client.InitAsync(new TBankInitPaymentRequest
        {
            Amount = TBankAmount.FromMinorUnits(15000),
            OrderId = "sp123",
            Description = "Multisplit payment"
        });

        Assert.Equal("https://example.test/v2/Init", handler.RequestUri?.ToString());
        Assert.Equal(HttpMethod.Post, handler.Method);
        Assert.Equal("7277900132", response.PaymentId);
        Assert.Equal(TBankPaymentStatus.New, response.Status);

        using var document = JsonDocument.Parse(handler.Body!);
        var root = document.RootElement;

        Assert.Equal("TerminalKey", root.GetProperty("TerminalKey").GetString());
        Assert.Equal(15000, root.GetProperty("Amount").GetInt64());
        Assert.Equal("sp123", root.GetProperty("OrderId").GetString());
        Assert.Equal("Multisplit payment", root.GetProperty("Description").GetString());
        Assert.Equal(
            "d4026f3432c8771934ca77afc4f028f7fec42350e777b66cd0e7398e6d6c7167",
            root.GetProperty("Token").GetString());
    }

    [Fact]
    public async Task GetStateAsync_PostsSignedRequestToGetStateEndpoint()
    {
        using var handler = new RecordingHandler("""
            {
              "Success": true,
              "ErrorCode": "0",
              "TerminalKey": "TestB",
              "Status": "CONFIRMED",
              "PaymentId": "20150",
              "OrderId": "order-1",
              "Amount": 1000
            }
            """);
        using var httpClient = new HttpClient(handler);
        var client = new TBankPaymentsClient(httpClient, new TBankPaymentsClientOptions
        {
            TerminalKey = "TestB",
            Password = "Dfsfh56dgKl",
            BaseAddress = new Uri("https://example.test/v2/")
        });

        var response = await client.GetStateAsync(new TBankPaymentStateRequest
        {
            PaymentId = "20150"
        });

        Assert.Equal("https://example.test/v2/GetState", handler.RequestUri?.ToString());
        Assert.Equal(TBankPaymentStatus.Confirmed, response.Status);

        using var document = JsonDocument.Parse(handler.Body!);
        var root = document.RootElement;

        Assert.Equal("TestB", root.GetProperty("TerminalKey").GetString());
        Assert.Equal("20150", root.GetProperty("PaymentId").GetString());
        Assert.Equal(
            "03acc0a77d6e870f402a1038c1ca5d8b4a985fe76f08016a869f10f2382bd7a9",
            root.GetProperty("Token").GetString());
    }

    [Fact]
    public async Task CheckOrderAsync_PostsSignedRequestToCheckOrderEndpoint()
    {
        using var handler = new RecordingHandler("""
            {
              "Success": true,
              "ErrorCode": "0",
              "Message": "OK",
              "OrderId": "21057",
              "TerminalKey": "TestB",
              "Payments": [
                {
                  "Status": "REJECTED",
                  "PaymentId": "10063",
                  "Rrn": 1234567,
                  "Amount": 555,
                  "Success": false,
                  "ErrorCode": 1051,
                  "Message": "Insufficient funds"
                },
                {
                  "Status": "NEW",
                  "PaymentId": "100553363",
                  "Rrn": "7654321",
                  "Amount": "555",
                  "Success": true,
                  "ErrorCode": "0",
                  "Message": "ok"
                }
              ]
            }
            """);
        using var httpClient = new HttpClient(handler);
        var client = new TBankPaymentsClient(httpClient, new TBankPaymentsClientOptions
        {
            TerminalKey = "TestB",
            Password = "Dfsfh56dgKl",
            BaseAddress = new Uri("https://example.test/v2/")
        });

        var response = await client.CheckOrderAsync(new TBankCheckOrderRequest
        {
            OrderId = "21057"
        });

        Assert.Equal("https://example.test/v2/CheckOrder", handler.RequestUri?.ToString());
        Assert.Equal("21057", response.OrderId);
        Assert.Equal("OK", response.Message);
        Assert.Equal(2, response.Payments.Count);
        Assert.Equal("10063", response.Payments[0].PaymentId);
        Assert.Equal("1234567", response.Payments[0].RRN);
        Assert.Equal("1051", response.Payments[0].ErrorCode);
        Assert.Equal(TBankPaymentStatus.Rejected, response.Payments[0].Status);
        Assert.Equal(TBankAmount.FromMinorUnits(555), response.Payments[1].Amount);
        Assert.Equal(TBankPaymentStatus.New, response.Payments[1].Status);

        using var document = JsonDocument.Parse(handler.Body!);
        var root = document.RootElement;

        Assert.Equal("TestB", root.GetProperty("TerminalKey").GetString());
        Assert.Equal("21057", root.GetProperty("OrderId").GetString());
        Assert.Equal(
            "48ccd85f66f0eeba1256458d3671e43efca5a93e4440772fb849ede2713d0c84",
            root.GetProperty("Token").GetString());
    }

    [Fact]
    public async Task CancelAsync_PostsSignedRequestToCancelEndpoint()
    {
        using var handler = new RecordingHandler("""
            {
              "Success": true,
              "ErrorCode": "0",
              "TerminalKey": "TestB",
              "Status": "REVERSED",
              "PaymentId": "20150",
              "OrderId": "order-1",
              "OriginalAmount": 1000,
              "NewAmount": 0
            }
            """);
        using var httpClient = new HttpClient(handler);
        var client = new TBankPaymentsClient(httpClient, new TBankPaymentsClientOptions
        {
            TerminalKey = "TestB",
            Password = "Dfsfh56dgKl",
            BaseAddress = new Uri("https://example.test/v2/")
        });

        var response = await client.CancelAsync(new TBankCancelPaymentRequest
        {
            PaymentId = "20150",
            Amount = TBankAmount.FromMinorUnits(1000),
            ExternalRequestId = "ext-1"
        });

        Assert.Equal("https://example.test/v2/Cancel", handler.RequestUri?.ToString());
        Assert.Equal(TBankPaymentStatus.Reversed, response.Status);
        Assert.Equal(TBankAmount.FromMinorUnits(1000), response.OriginalAmount);
        Assert.Equal(TBankAmount.FromMinorUnits(0), response.NewAmount);

        using var document = JsonDocument.Parse(handler.Body!);
        var root = document.RootElement;

        Assert.Equal("TestB", root.GetProperty("TerminalKey").GetString());
        Assert.Equal("20150", root.GetProperty("PaymentId").GetString());
        Assert.Equal(1000, root.GetProperty("Amount").GetInt64());
        Assert.Equal("ext-1", root.GetProperty("ExternalRequestId").GetString());
        Assert.Equal(
            "9ae3bebd6af425702ca9f87325c82a2bdd3105cf2e6b05209a4baa668945de70",
            root.GetProperty("Token").GetString());
    }

    [Fact]
    public async Task ConfirmAsync_PostsSignedRequestToConfirmEndpoint()
    {
        using var handler = new RecordingHandler("""
            {
              "Success": true,
              "ErrorCode": "0",
              "TerminalKey": "TestB",
              "Status": "CONFIRMED",
              "PaymentId": "20150",
              "OrderId": "order-1",
              "Amount": 1000
            }
            """);
        using var httpClient = new HttpClient(handler);
        var client = new TBankPaymentsClient(httpClient, new TBankPaymentsClientOptions
        {
            TerminalKey = "TestB",
            Password = "Dfsfh56dgKl",
            BaseAddress = new Uri("https://example.test/v2/")
        });

        var response = await client.ConfirmAsync(new TBankConfirmPaymentRequest
        {
            PaymentId = "20150",
            Amount = TBankAmount.FromMinorUnits(1000)
        });

        Assert.Equal("https://example.test/v2/Confirm", handler.RequestUri?.ToString());
        Assert.Equal(TBankPaymentStatus.Confirmed, response.Status);
        Assert.Equal(TBankAmount.FromMinorUnits(1000), response.Amount);

        using var document = JsonDocument.Parse(handler.Body!);
        var root = document.RootElement;

        Assert.Equal("TestB", root.GetProperty("TerminalKey").GetString());
        Assert.Equal("20150", root.GetProperty("PaymentId").GetString());
        Assert.Equal(1000, root.GetProperty("Amount").GetInt64());
        Assert.Equal(
            "599e64ab2b7ff5067cd8f18e4b746ae716315765b733a9bd917acdf7b92d06b4",
            root.GetProperty("Token").GetString());
    }

    [Fact]
    public async Task GetQrAsync_PostsSignedRequestToGetQrEndpoint()
    {
        using var handler = new RecordingHandler("""
            {
              "Success": true,
              "ErrorCode": "0",
              "TerminalKey": "TestB",
              "OrderId": "order-1",
              "PaymentId": 20150,
              "Data": "https://qr.nspk.ru/example"
            }
            """);
        using var httpClient = new HttpClient(handler);
        var client = new TBankPaymentsClient(httpClient, new TBankPaymentsClientOptions
        {
            TerminalKey = "TestB",
            Password = "Dfsfh56dgKl",
            BaseAddress = new Uri("https://example.test/v2/")
        });

        var response = await client.GetQrAsync(new TBankQrRequest
        {
            PaymentId = "20150",
            DataType = TBankQrDataType.Image
        });

        Assert.Equal("https://example.test/v2/GetQr", handler.RequestUri?.ToString());
        Assert.Equal("https://qr.nspk.ru/example", response.Data);
        Assert.Equal("20150", response.PaymentId);

        using var document = JsonDocument.Parse(handler.Body!);
        var root = document.RootElement;

        Assert.Equal("TestB", root.GetProperty("TerminalKey").GetString());
        Assert.Equal("20150", root.GetProperty("PaymentId").GetString());
        Assert.Equal("IMAGE", root.GetProperty("DataType").GetString());
        Assert.Equal(
            "a65eecdd9fc78ae498777ce3a89462c5f5cdb996511aa3076f0e9e1c42d8b5d9",
            root.GetProperty("Token").GetString());
    }

    [Fact]
    public async Task ChargeQrAsync_PostsSignedRequestToChargeQrEndpoint()
    {
        using var handler = new RecordingHandler("""
            {
              "Success": true,
              "ErrorCode": "0",
              "TerminalKey": "TestB",
              "OrderId": "order-1",
              "PaymentId": "20150",
              "Status": "PAY_CHECKING",
              "Amount": 1000,
              "Currency": "643"
            }
            """);
        using var httpClient = new HttpClient(handler);
        var client = new TBankPaymentsClient(httpClient, new TBankPaymentsClientOptions
        {
            TerminalKey = "TestB",
            Password = "Dfsfh56dgKl",
            BaseAddress = new Uri("https://example.test/v2/")
        });

        var response = await client.ChargeQrAsync(new TBankChargeQrRequest
        {
            PaymentId = "20150",
            AccountToken = "acct-1",
            SendEmail = true,
            InfoEmail = "customer@example.test"
        });

        Assert.Equal("https://example.test/v2/ChargeQr", handler.RequestUri?.ToString());
        Assert.Equal(TBankPaymentStatus.PayChecking, response.Status);
        Assert.Equal(TBankAmount.FromMinorUnits(1000), response.Amount);
        Assert.Equal(643, response.Currency);

        using var document = JsonDocument.Parse(handler.Body!);
        var root = document.RootElement;

        Assert.Equal("TestB", root.GetProperty("TerminalKey").GetString());
        Assert.Equal("20150", root.GetProperty("PaymentId").GetString());
        Assert.Equal("acct-1", root.GetProperty("AccountToken").GetString());
        Assert.True(root.GetProperty("SendEmail").GetBoolean());
        Assert.Equal("customer@example.test", root.GetProperty("InfoEmail").GetString());
        Assert.Equal(
            "dd6f4cfd626942fb1e15ca756bb5bbb8a7c900ee921377e1bf31aace3f2148b9",
            root.GetProperty("Token").GetString());
    }

    [Fact]
    public async Task InitAsync_ReturnsTypedResponseForTBankApiError()
    {
        using var handler = new RecordingHandler("""
            {
              "Success": false,
              "ErrorCode": "204",
              "Message": "Invalid token",
              "Details": "Check TerminalKey or password"
            }
            """, HttpStatusCode.BadRequest);
        using var httpClient = new HttpClient(handler);
        var client = new TBankPaymentsClient(httpClient, new TBankPaymentsClientOptions
        {
            TerminalKey = "TerminalKey",
            Password = "Password",
            BaseAddress = new Uri("https://example.test/v2/")
        });

        var response = await client.InitAsync(new TBankInitPaymentRequest
        {
            Amount = TBankAmount.FromMinorUnits(15000),
            OrderId = "sp123"
        });

        Assert.False(response.Success);
        Assert.Equal("204", response.ErrorCode);
        Assert.Equal("Invalid token", response.Message);
        Assert.Equal("Check TerminalKey or password", response.Details);
        Assert.Equal(HttpStatusCode.BadRequest, response.Metadata?.HttpStatusCode);
        Assert.Null(response.Metadata?.RawResponseBody);
    }

    [Fact]
    public async Task InitAsync_AttachesResponseMetadataWithoutRawBodyByDefault()
    {
        using var handler = new RecordingHandler("""
            {
              "Success": true,
              "ErrorCode": "0",
              "TerminalKey": "TerminalKey",
              "Status": "NEW",
              "PaymentId": "7277900132",
              "OrderId": "sp123",
              "Amount": 15000
            }
            """);
        handler.ResponseHeaders["X-Request-Id"] = "request-1";
        handler.ContentHeaders["X-Body-Trace"] = "body-1";
        using var httpClient = new HttpClient(handler);
        var client = new TBankPaymentsClient(httpClient, new TBankPaymentsClientOptions
        {
            TerminalKey = "TerminalKey",
            Password = "Password",
            BaseAddress = new Uri("https://example.test/v2/")
        });

        var response = await client.InitAsync(new TBankInitPaymentRequest
        {
            Amount = TBankAmount.FromMinorUnits(15000),
            OrderId = "sp123"
        });

        Assert.NotNull(response.Metadata);
        Assert.Equal(HttpStatusCode.OK, response.Metadata.HttpStatusCode);
        Assert.Equal("request-1", Assert.Single(response.Metadata.Headers["X-Request-Id"]));
        Assert.Equal("body-1", Assert.Single(response.Metadata.Headers["X-Body-Trace"]));
        Assert.Null(response.Metadata.RawResponseBody);
    }

    [Fact]
    public async Task InitAsync_CapturesRawResponseBodyWhenEnabled()
    {
        const string responseBody = """
            {
              "Success": true,
              "ErrorCode": "0",
              "TerminalKey": "TerminalKey",
              "Status": "NEW",
              "PaymentId": "7277900132",
              "OrderId": "sp123",
              "Amount": 15000
            }
            """;
        using var handler = new RecordingHandler(responseBody);
        using var httpClient = new HttpClient(handler);
        var client = new TBankPaymentsClient(httpClient, new TBankPaymentsClientOptions
        {
            TerminalKey = "TerminalKey",
            Password = "Password",
            BaseAddress = new Uri("https://example.test/v2/"),
            CaptureRawResponseBody = true
        });

        var response = await client.InitAsync(new TBankInitPaymentRequest
        {
            Amount = TBankAmount.FromMinorUnits(15000),
            OrderId = "sp123"
        });

        Assert.Equal(responseBody, response.Metadata?.RawResponseBody);
    }

    [Fact]
    public async Task InitAsync_ThrowsApiExceptionForTBankApiErrorInStrictMode()
    {
        using var handler = new RecordingHandler("""
            {
              "Success": false,
              "ErrorCode": "204",
              "Message": "Invalid token",
              "Details": "Check TerminalKey or password"
            }
            """, HttpStatusCode.BadRequest);
        using var httpClient = new HttpClient(handler);
        var client = new TBankPaymentsClient(httpClient, new TBankPaymentsClientOptions
        {
            TerminalKey = "TerminalKey",
            Password = "Password",
            BaseAddress = new Uri("https://example.test/v2/"),
            ThrowOnTBankApiError = true
        });

        var exception = await Assert.ThrowsAsync<TBankAcquiringApiException>(() => client.InitAsync(new TBankInitPaymentRequest
        {
            Amount = TBankAmount.FromMinorUnits(15000),
            OrderId = "sp123"
        }));

        Assert.Equal("204", exception.ErrorCode);
        Assert.Equal("Invalid token", exception.ErrorMessage);
        Assert.Equal("Check TerminalKey or password", exception.Details);
        Assert.Equal(HttpStatusCode.BadRequest, exception.HttpStatusCode);
    }

    [Fact]
    public async Task InitAsync_ThrowsProtocolExceptionForMalformedJsonResponse()
    {
        using var handler = new RecordingHandler("""
            {"Token":"secret-token","ErrorCode":
            """);
        using var httpClient = new HttpClient(handler);
        var client = new TBankPaymentsClient(httpClient, new TBankPaymentsClientOptions
        {
            TerminalKey = "TerminalKey",
            Password = "Password",
            BaseAddress = new Uri("https://example.test/v2/")
        });

        var exception = await Assert.ThrowsAsync<TBankAcquiringProtocolException>(() => client.InitAsync(new TBankInitPaymentRequest
        {
            Amount = TBankAmount.FromMinorUnits(15000),
            OrderId = "sp123"
        }));

        Assert.Equal(HttpStatusCode.OK, exception.HttpStatusCode);
        Assert.Contains("***REDACTED***", exception.ResponseBodyPreview);
        Assert.DoesNotContain("secret-token", exception.ResponseBodyPreview);
    }

    [Fact]
    public async Task InitAsync_ThrowsProtocolExceptionForEmptyResponse()
    {
        using var handler = new RecordingHandler("");
        using var httpClient = new HttpClient(handler);
        var client = new TBankPaymentsClient(httpClient, new TBankPaymentsClientOptions
        {
            TerminalKey = "TerminalKey",
            Password = "Password",
            BaseAddress = new Uri("https://example.test/v2/")
        });

        var exception = await Assert.ThrowsAsync<TBankAcquiringProtocolException>(() => client.InitAsync(new TBankInitPaymentRequest
        {
            Amount = TBankAmount.FromMinorUnits(15000),
            OrderId = "sp123"
        }));

        Assert.Equal(HttpStatusCode.OK, exception.HttpStatusCode);
    }

    [Fact]
    public async Task InitAsync_WrapsTransportException()
    {
        using var httpClient = new HttpClient(new ThrowingHandler());
        var client = new TBankPaymentsClient(httpClient, new TBankPaymentsClientOptions
        {
            TerminalKey = "TerminalKey",
            Password = "Password",
            BaseAddress = new Uri("https://example.test/v2/")
        });

        var exception = await Assert.ThrowsAsync<TBankAcquiringTransportException>(() => client.InitAsync(new TBankInitPaymentRequest
        {
            Amount = TBankAmount.FromMinorUnits(15000),
            OrderId = "sp123"
        }));

        Assert.IsType<HttpRequestException>(exception.InnerException);
    }

    private sealed class RecordingHandler(string responseBody, HttpStatusCode statusCode = HttpStatusCode.OK) : HttpMessageHandler
    {
        public Dictionary<string, string> ResponseHeaders { get; } = [];

        public Dictionary<string, string> ContentHeaders { get; } = [];

        public Uri? RequestUri { get; private set; }

        public HttpMethod? Method { get; private set; }

        public string? Body { get; private set; }

        protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            RequestUri = request.RequestUri;
            Method = request.Method;
            Body = request.Content is null ? null : await request.Content.ReadAsStringAsync(cancellationToken);

            var response = new HttpResponseMessage(statusCode)
            {
                Content = new StringContent(responseBody)
            };

            foreach (var header in ResponseHeaders)
            {
                response.Headers.TryAddWithoutValidation(header.Key, header.Value);
            }

            foreach (var header in ContentHeaders)
            {
                response.Content.Headers.TryAddWithoutValidation(header.Key, header.Value);
            }

            return response;
        }
    }

    private sealed class ThrowingHandler : HttpMessageHandler
    {
        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            throw new HttpRequestException("Network unavailable.");
        }
    }
}
