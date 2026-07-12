using System.Net;
using System.Text.Json;
using TBankAcquiringNet;

namespace TBankAcquiringNet.Tests;

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
        Assert.Equal(TBankPaymentStatus.NEW, response.Status);

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
    public async Task InitAsync_SerializesTypedEnumFieldsToWireValues()
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
        using var httpClient = new HttpClient(handler);
        var client = new TBankPaymentsClient(httpClient, new TBankPaymentsClientOptions
        {
            TerminalKey = "TerminalKey",
            Password = "Password",
            BaseAddress = new Uri("https://example.test/v2/")
        });

        await client.InitAsync(new TBankInitPaymentRequest
        {
            Amount = TBankAmount.FromMinorUnits(15000),
            OrderId = "sp123",
            PayType = TBankPayType.TwoStage,
            Language = TBankLanguage.En,
            Recurrent = TBankRecurrent.Yes
        });

        using var document = JsonDocument.Parse(handler.Body!);
        var root = document.RootElement;

        Assert.Equal("T", root.GetProperty("PayType").GetString());
        Assert.Equal("en", root.GetProperty("Language").GetString());
        Assert.Equal("Y", root.GetProperty("Recurrent").GetString());
        Assert.Equal(
            "3e204ffb958888570b8514144106c87713b789053d6d17ad89824c78392c1e3f",
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
        Assert.Equal(TBankPaymentStatus.CONFIRMED, response.Status);

        using var document = JsonDocument.Parse(handler.Body!);
        var root = document.RootElement;

        Assert.Equal("TestB", root.GetProperty("TerminalKey").GetString());
        Assert.Equal("20150", root.GetProperty("PaymentId").GetString());
        Assert.Equal(
            "03acc0a77d6e870f402a1038c1ca5d8b4a985fe76f08016a869f10f2382bd7a9",
            root.GetProperty("Token").GetString());
    }

    [Fact]
    public async Task GetStateAsync_ThrowsNotImplementedForUnknownStatus()
    {
        using var handler = new RecordingHandler("""
            {
              "Success": true,
              "ErrorCode": "0",
              "TerminalKey": "TestB",
              "Status": "FUTURE_STATUS",
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

        var exception = await Assert.ThrowsAsync<NotImplementedException>(() => client.GetStateAsync(new TBankPaymentStateRequest
        {
            PaymentId = "20150"
        }));

        Assert.Contains("FUTURE_STATUS", exception.Message);
        Assert.Contains("github.com/ai-iskuzhin/TBankAcquiringNet/issues/new", exception.Message);
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
        Assert.Equal(TBankPaymentStatus.REJECTED, response.Payments[0].Status);
        Assert.Equal(TBankAmount.FromMinorUnits(555), response.Payments[1].Amount);
        Assert.Equal(TBankPaymentStatus.NEW, response.Payments[1].Status);

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
        Assert.Equal(TBankPaymentStatus.REVERSED, response.Status);
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
        Assert.Equal(TBankPaymentStatus.CONFIRMED, response.Status);
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
        Assert.Equal(TBankPaymentStatus.PAY_CHECKING, response.Status);
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

    [Fact]
    public async Task GetQrStateAsync_PostsSignedRequestToGetQrStateEndpoint()
    {
        using var handler = new RecordingHandler("""
            {
              "Success": true,
              "ErrorCode": "0",
              "Status": "CONFIRMED",
              "QrCancelCode": "I05043",
              "QrCancelMessage": "У покупателя нет расчетного счета в этом банке.",
              "OrderId": "7830122",
              "Amount": 10000,
              "Message": "OK"
            }
            """);
        using var httpClient = new HttpClient(handler);
        var client = new TBankPaymentsClient(httpClient, new TBankPaymentsClientOptions
        {
            TerminalKey = "TestB",
            Password = "Dfsfh56dgKl",
            BaseAddress = new Uri("https://example.test/v2/")
        });

        var response = await client.GetQrStateAsync(new TBankQrStateRequest
        {
            PaymentId = "20150"
        });

        Assert.Equal("https://example.test/v2/GetQrState", handler.RequestUri?.ToString());
        Assert.Equal(TBankPaymentStatus.CONFIRMED, response.Status);
        Assert.Equal("I05043", response.QrCancelCode);
        Assert.Equal("7830122", response.OrderId);
        Assert.Equal(TBankAmount.FromMinorUnits(10000), response.Amount);

        using var document = JsonDocument.Parse(handler.Body!);
        var root = document.RootElement;

        Assert.Equal("TestB", root.GetProperty("TerminalKey").GetString());
        Assert.Equal("20150", root.GetProperty("PaymentId").GetString());
        Assert.Equal(
            "03acc0a77d6e870f402a1038c1ca5d8b4a985fe76f08016a869f10f2382bd7a9",
            root.GetProperty("Token").GetString());
    }

    [Fact]
    public async Task GetQrBankListAsync_PostsSignedRequestToGetQrBankListEndpoint()
    {
        using var handler = new RecordingHandler("""
            {
              "Success": true,
              "ErrorCode": "0",
              "Message": "OK",
              "BankList": [
                { "BankId": "3fa85f64-5717-4562-b3fc-2c963f66afa6", "NspkBankId": "100000000004", "BankName": "Т-Банк", "BankLogo": "https://qr.nspk.ru/logo/bank100000000004.png", "BankOrder": 1 },
                { "BankId": "3fa85f64-5717-4562-b3fc-2c963f66afa6", "NspkBankId": "100000000111", "BankName": "Сбербанк", "BankLogo": "https://qr.nspk.ru/logo/bank100000000111.png", "BankOrder": 2 },
                { "BankId": "3fa85f64-5717-4562-b3fc-2c963f66afa6", "NspkBankId": "100000000005", "BankName": "Банк ВТБ", "BankLogo": "https://qr.nspk.ru/logo/bank100000000005.png", "BankOrder": 3 }
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

        var response = await client.GetQrBankListAsync(new TBankQrBankListRequest
        {
            ScenarioType = "qr",
            Device = new TBankQrDevice { Type = "mobile", Os = "iOS" }
        });

        Assert.Equal("https://example.test/v2/GetQrBankList", handler.RequestUri?.ToString());
        Assert.Equal(3, response.BankList.Count);
        Assert.Equal("Т-Банк", response.BankList[0].BankName);
        Assert.Equal("100000000004", response.BankList[0].NspkBankId);
        Assert.Equal(1, response.BankList[0].BankOrder);

        using var document = JsonDocument.Parse(handler.Body!);
        var root = document.RootElement;

        Assert.Equal("TestB", root.GetProperty("TerminalKey").GetString());
        Assert.Equal("qr", root.GetProperty("ScenarioType").GetString());
        Assert.Equal("mobile", root.GetProperty("Device").GetProperty("Type").GetString());
        Assert.Equal("iOS", root.GetProperty("Device").GetProperty("Os").GetString());
        Assert.Equal(
            "06f2f2eee91fc71b8a6d88e0408a2d3871d3f6422e7707192f2449fdcc254d81",
            root.GetProperty("Token").GetString());
    }

    [Fact]
    public async Task GetAccountQrListAsync_PostsSignedRequestToGetAccountQrListEndpoint()
    {
        using var handler = new RecordingHandler("""
            {
              "TerminalKey": "TestB",
              "Success": true,
              "ErrorCode": "0",
              "Message": "OK",
              "AccountTokens": [
                { "RequestKey": "77520", "Status": "ACTIVE", "AccountToken": "0b67f2cae19b41809f85d5674de30a1a", "BankMemberId": "100000000004", "BankMemberName": "T-Банк" },
                { "RequestKey": "77563", "Status": "ACTIVE", "AccountToken": "14ac4445811e8225db8ed312j4433a68", "BankMemberId": "100000000004", "BankMemberName": "T-Банк" },
                { "RequestKey": "77644", "Status": "PROCCESING" }
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

        var response = await client.GetAccountQrListAsync(new TBankAccountQrListRequest());

        Assert.Equal("https://example.test/v2/GetAccountQrList", handler.RequestUri?.ToString());
        Assert.Equal(3, response.AccountTokens.Count);
        Assert.Equal("77520", response.AccountTokens[0].RequestKey);
        Assert.Equal(TBankAccountQrStatus.ACTIVE, response.AccountTokens[0].Status);
        Assert.Equal("0b67f2cae19b41809f85d5674de30a1a", response.AccountTokens[0].AccountToken);
        Assert.Equal(TBankAccountQrStatus.PROCESSING, response.AccountTokens[2].Status);

        using var document = JsonDocument.Parse(handler.Body!);
        var root = document.RootElement;

        Assert.Equal("TestB", root.GetProperty("TerminalKey").GetString());
        Assert.Equal(
            "34d48df4308e9325eed6f5d3744c4379213a110183b203ed21a9d984cb965f8a",
            root.GetProperty("Token").GetString());
    }

    [Fact]
    public async Task GetAddAccountQrStateAsync_PostsSignedRequestToGetAddAccountQrStateEndpoint()
    {
        using var handler = new RecordingHandler("""
            {
              "TerminalKey": "TestB",
              "RequestKey": 211258,
              "BankMemberId": "100000000004",
              "BankMemberName": "T-Банк",
              "AccountToken": "a022254a5c3c46a7327c8a12cb5c8389",
              "Success": true,
              "Status": "ACTIVE",
              "ErrorCode": "0",
              "Message": "OK"
            }
            """);
        using var httpClient = new HttpClient(handler);
        var client = new TBankPaymentsClient(httpClient, new TBankPaymentsClientOptions
        {
            TerminalKey = "TestB",
            Password = "Dfsfh56dgKl",
            BaseAddress = new Uri("https://example.test/v2/")
        });

        var response = await client.GetAddAccountQrStateAsync(new TBankAddAccountQrStateRequest
        {
            RequestKey = "13021"
        });

        Assert.Equal("https://example.test/v2/GetAddAccountQrState", handler.RequestUri?.ToString());
        Assert.Equal("211258", response.RequestKey);
        Assert.Equal(TBankAccountQrStatus.ACTIVE, response.Status);
        Assert.Equal("a022254a5c3c46a7327c8a12cb5c8389", response.AccountToken);
        Assert.Equal("100000000004", response.BankMemberId);

        using var document = JsonDocument.Parse(handler.Body!);
        var root = document.RootElement;

        Assert.Equal("TestB", root.GetProperty("TerminalKey").GetString());
        Assert.Equal("13021", root.GetProperty("RequestKey").GetString());
        Assert.Equal(
            "1abf20ddd4f1bd4879c07193572a5c11a0c0229cdd2abefa1bb25974879cf0a1",
            root.GetProperty("Token").GetString());
    }

    [Fact]
    public async Task AddAccountQrAsync_PostsSignedRequestToAddAccountQrEndpoint()
    {
        using var handler = new RecordingHandler("""
            {
              "TerminalKey": "TestB",
              "Description": "bind",
              "DataType": "PAYLOAD",
              "Data": "https://sub.nspk.ru/AB50803R2RH0LJ2A9RTU038L6NT5RU1M?type=03",
              "RequestKey": "ed989549-d3be-4758-95c7-22647e03f9ec",
              "ErrorCode": "0",
              "Success": true,
              "Message": "OK"
            }
            """);
        using var httpClient = new HttpClient(handler);
        var client = new TBankPaymentsClient(httpClient, new TBankPaymentsClientOptions
        {
            TerminalKey = "TestB",
            Password = "Dfsfh56dgKl",
            BaseAddress = new Uri("https://example.test/v2/")
        });

        var response = await client.AddAccountQrAsync(new TBankAddAccountQrRequest
        {
            Description = "bind",
            DataType = TBankQrDataType.Payload
        });

        Assert.Equal("https://example.test/v2/AddAccountQr", handler.RequestUri?.ToString());
        Assert.Equal(TBankQrDataType.Payload, response.DataType);
        Assert.StartsWith("https://sub.nspk.ru/", response.Data);
        Assert.Equal("ed989549-d3be-4758-95c7-22647e03f9ec", response.RequestKey);

        using var document = JsonDocument.Parse(handler.Body!);
        var root = document.RootElement;

        Assert.Equal("TestB", root.GetProperty("TerminalKey").GetString());
        Assert.Equal("bind", root.GetProperty("Description").GetString());
        Assert.Equal("PAYLOAD", root.GetProperty("DataType").GetString());
        Assert.Equal(
            "f8fe8633cf88301c896f30f7f5c635158635946adbf7381766b34219170259b1",
            root.GetProperty("Token").GetString());
    }

    [Fact]
    public async Task QrMembersListAsync_PostsSignedRequestToQrMembersListEndpoint()
    {
        using var handler = new RecordingHandler("""
            {
              "Members": [
                { "MemberId": "1000000", "MemberName": "T-Банк", "IsPayee": true }
              ],
              "OrderId": "21050",
              "Success": true,
              "ErrorCode": "0",
              "Message": "OK"
            }
            """);
        using var httpClient = new HttpClient(handler);
        var client = new TBankPaymentsClient(httpClient, new TBankPaymentsClientOptions
        {
            TerminalKey = "TestB",
            Password = "Dfsfh56dgKl",
            BaseAddress = new Uri("https://example.test/v2/")
        });

        var response = await client.QrMembersListAsync(new TBankQrMembersListRequest
        {
            PaymentId = "10063"
        });

        Assert.Equal("https://example.test/v2/QrMembersList", handler.RequestUri?.ToString());
        Assert.Equal("21050", response.OrderId);
        Assert.Single(response.Members);
        Assert.Equal("1000000", response.Members[0].MemberId);
        Assert.True(response.Members[0].IsPayee);

        using var document = JsonDocument.Parse(handler.Body!);
        var root = document.RootElement;

        Assert.Equal("TestB", root.GetProperty("TerminalKey").GetString());
        Assert.Equal("10063", root.GetProperty("PaymentId").GetString());
        Assert.Equal(
            "65f05b9d7695edb5875afd98e2fec8ae8299f8a9412e8296c744942e618c912c",
            root.GetProperty("Token").GetString());
    }

    [Fact]
    public async Task SbpPayTestAsync_PostsSignedRequestToSbpPayTestEndpoint()
    {
        using var handler = new RecordingHandler("""
            {
              "Success": true,
              "ErrorCode": "0",
              "Message": "OK",
              "Details": "0"
            }
            """);
        using var httpClient = new HttpClient(handler);
        var client = new TBankPaymentsClient(httpClient, new TBankPaymentsClientOptions
        {
            TerminalKey = "TestB",
            Password = "Dfsfh56dgKl",
            BaseAddress = new Uri("https://example.test/v2/")
        });

        var response = await client.SbpPayTestAsync(new TBankSbpPayTestRequest
        {
            PaymentId = "13660",
            IsDeadlineExpired = true,
            IsRejected = false
        });

        Assert.Equal("https://example.test/v2/SbpPayTest", handler.RequestUri?.ToString());
        Assert.True(response.Success);
        Assert.Equal("OK", response.Message);
        Assert.Equal("0", response.Details);

        using var document = JsonDocument.Parse(handler.Body!);
        var root = document.RootElement;

        Assert.Equal("TestB", root.GetProperty("TerminalKey").GetString());
        Assert.Equal("13660", root.GetProperty("PaymentId").GetString());
        Assert.True(root.GetProperty("IsDeadlineExpired").GetBoolean());
        Assert.False(root.GetProperty("IsRejected").GetBoolean());
        Assert.Equal(
            "9374ff65eb8f74c8a62dbe7c36c186aaf82fa785640d7e41d944d0eb5b721dd0",
            root.GetProperty("Token").GetString());
    }

    [Fact]
    public async Task InitAsync_SendsVersionedUserAgentHeader()
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
        using var httpClient = new HttpClient(handler);
        var client = new TBankPaymentsClient(httpClient, new TBankPaymentsClientOptions
        {
            TerminalKey = "TerminalKey",
            Password = "Password",
            BaseAddress = new Uri("https://example.test/v2/")
        });

        await client.InitAsync(new TBankInitPaymentRequest
        {
            Amount = TBankAmount.FromMinorUnits(15000),
            OrderId = "sp123"
        });

        Assert.StartsWith("TBankAcquiringNet/", handler.UserAgent);
        Assert.Matches(@"^TBankAcquiringNet/\d+\.\d+\.\d+", handler.UserAgent!);
    }

    [Fact]
    public async Task GetTinkoffPayStatusAsync_GetsTinkoffPayStatusEndpoint()
    {
        using var handler = new RecordingHandler("""
            {
              "Params": { "Allowed": true, "Version": "1.0" },
              "Success": true,
              "ErrorCode": "0",
              "Message": "OK"
            }
            """);
        using var httpClient = new HttpClient(handler);
        var client = new TBankPaymentsClient(httpClient, new TBankPaymentsClientOptions
        {
            TerminalKey = "TestB",
            Password = "Dfsfh56dgKl",
            BaseAddress = new Uri("https://example.test/v2/")
        });

        var response = await client.GetTinkoffPayStatusAsync();

        Assert.Equal(HttpMethod.Get, handler.Method);
        Assert.Equal("https://example.test/v2/TinkoffPay/terminals/TestB/status", handler.RequestUri?.ToString());
        Assert.Null(handler.Body);
        Assert.True(response.Success);
        Assert.NotNull(response.Params);
        Assert.True(response.Params!.Allowed);
        Assert.Equal("1.0", response.Params.Version);
    }

    [Fact]
    public async Task GetTinkoffPayLinkAsync_GetsTinkoffPayLinkEndpoint()
    {
        using var handler = new RecordingHandler("""
            {
              "Params": { "RedirectUrl": "https://o.tbank.ru/tpay/req123", "WebQR": "http://example.com" },
              "Success": true,
              "ErrorCode": "0"
            }
            """);
        using var httpClient = new HttpClient(handler);
        var client = new TBankPaymentsClient(httpClient, new TBankPaymentsClientOptions
        {
            TerminalKey = "TestB",
            Password = "Dfsfh56dgKl",
            BaseAddress = new Uri("https://example.test/v2/")
        });

        var response = await client.GetTinkoffPayLinkAsync("700031849", "2.0");

        Assert.Equal(HttpMethod.Get, handler.Method);
        Assert.Equal("https://example.test/v2/TinkoffPay/transactions/700031849/versions/2.0/link", handler.RequestUri?.ToString());
        Assert.Equal("https://o.tbank.ru/tpay/req123", response.Params?.RedirectUrl);
        Assert.Equal("http://example.com", response.Params?.WebQR);
    }

    [Fact]
    public async Task GetTinkoffPayQrAsync_ReturnsSvg()
    {
        const string svg = "<svg xmlns=\"http://www.w3.org/2000/svg\" width=\"124\" height=\"124\"></svg>";
        using var handler = new RecordingHandler(svg);
        using var httpClient = new HttpClient(handler);
        var client = new TBankPaymentsClient(httpClient, new TBankPaymentsClientOptions
        {
            TerminalKey = "TestB",
            Password = "Dfsfh56dgKl",
            BaseAddress = new Uri("https://example.test/v2/")
        });

        var result = await client.GetTinkoffPayQrAsync("700031849");

        Assert.Equal(HttpMethod.Get, handler.Method);
        Assert.Equal("https://example.test/v2/TinkoffPay/700031849/QR", handler.RequestUri?.ToString());
        // Must be the registered IANA type; "image/svg" is rejected by T-Bank with HTTP 415.
        Assert.Equal("image/svg+xml", handler.Accept);
        Assert.Equal(svg, result);
    }

    [Fact]
    public async Task GetSberPayQrAsync_ReturnsSvg()
    {
        const string svg = "<svg xmlns=\"http://www.w3.org/2000/svg\" width=\"124\" height=\"124\"></svg>";
        using var handler = new RecordingHandler(svg);
        using var httpClient = new HttpClient(handler);
        var client = new TBankPaymentsClient(httpClient, new TBankPaymentsClientOptions
        {
            TerminalKey = "TestB",
            Password = "Dfsfh56dgKl",
            BaseAddress = new Uri("https://example.test/v2/")
        });

        var result = await client.GetSberPayQrAsync("700031849");

        Assert.Equal(HttpMethod.Get, handler.Method);
        Assert.Equal("https://example.test/v2/SberPay/700031849/QR", handler.RequestUri?.ToString());
        Assert.Equal("image/svg+xml", handler.Accept);
        Assert.Equal(svg, result);
    }

    [Fact]
    public async Task GetQrSvg_ThrowsWhenBodyIsJsonErrorEnvelope()
    {
        // T-Bank can answer HTTP 200 with a JSON error instead of an SVG (e.g. an expired payment).
        using var handler = new RecordingHandler("""
            {"Success":false,"ErrorCode":"8","Message":"Неверный статус транзакции.","Details":null}
            """);
        using var httpClient = new HttpClient(handler);
        var client = new TBankPaymentsClient(httpClient, new TBankPaymentsClientOptions
        {
            TerminalKey = "TestB",
            Password = "Dfsfh56dgKl",
            BaseAddress = new Uri("https://example.test/v2/")
        });

        var exception = await Assert.ThrowsAsync<TBankAcquiringProtocolException>(
            () => client.GetTinkoffPayQrAsync("8819955040"));

        Assert.Contains("ErrorCode '8'", exception.Message);
        Assert.Contains("Неверный статус транзакции.", exception.Message);
        Assert.Contains("Неверный статус транзакции.", exception.ResponseBodyPreview!);
    }

    [Fact]
    public async Task GetSberPayLinkAsync_GetsSberPayLinkEndpoint()
    {
        using var handler = new RecordingHandler("""
            {
              "Params": { "RedirectUrl": "tinkoffbank://Main/EInvoicing?billId=5000015507" },
              "Success": true,
              "ErrorCode": "0"
            }
            """);
        using var httpClient = new HttpClient(handler);
        var client = new TBankPaymentsClient(httpClient, new TBankPaymentsClientOptions
        {
            TerminalKey = "TestB",
            Password = "Dfsfh56dgKl",
            BaseAddress = new Uri("https://example.test/v2/")
        });

        var response = await client.GetSberPayLinkAsync("700031849");

        Assert.Equal(HttpMethod.Get, handler.Method);
        Assert.Equal("https://example.test/v2/SberPay/transactions/700031849/link", handler.RequestUri?.ToString());
        Assert.StartsWith("tinkoffbank://", response.Params?.RedirectUrl);
    }

    [Fact]
    public async Task GetMirPayDeepLinkAsync_PostsSignedRequestToMirPayEndpoint()
    {
        using var handler = new RecordingHandler("""
            {
              "Success": true,
              "ErrorCode": "0",
              "Deeplink": "mirpay://pay.mironline.ru/inapp/eyJhbGciOiJQUzI1NiJ9",
              "Message": "string"
            }
            """);
        using var httpClient = new HttpClient(handler);
        var client = new TBankPaymentsClient(httpClient, new TBankPaymentsClientOptions
        {
            TerminalKey = "TestB",
            Password = "Dfsfh56dgKl",
            BaseAddress = new Uri("https://example.test/v2/")
        });

        var response = await client.GetMirPayDeepLinkAsync(new TBankMirPayDeepLinkRequest
        {
            PaymentId = "20150"
        });

        Assert.Equal(HttpMethod.Post, handler.Method);
        Assert.Equal("https://example.test/v2/MirPay/GetDeepLink", handler.RequestUri?.ToString());
        Assert.StartsWith("mirpay://", response.Deeplink);

        using var document = JsonDocument.Parse(handler.Body!);
        var root = document.RootElement;

        Assert.Equal("TestB", root.GetProperty("TerminalKey").GetString());
        Assert.Equal("20150", root.GetProperty("PaymentId").GetString());
        Assert.Equal(
            "03acc0a77d6e870f402a1038c1ca5d8b4a985fe76f08016a869f10f2382bd7a9",
            root.GetProperty("Token").GetString());
    }

    [Fact]
    public async Task GetAlfaPayLinkAsync_PostsSignedRequestToAlfaPayEndpoint()
    {
        using var handler = new RecordingHandler("""
            {
              "Params": { "RedirectUrl": "https://payment-app.com" },
              "Success": true,
              "ErrorCode": "0"
            }
            """);
        using var httpClient = new HttpClient(handler);
        var client = new TBankPaymentsClient(httpClient, new TBankPaymentsClientOptions
        {
            TerminalKey = "TestB",
            Password = "Dfsfh56dgKl",
            BaseAddress = new Uri("https://example.test/v2/")
        });

        var response = await client.GetAlfaPayLinkAsync(new TBankAlfaPayLinkRequest
        {
            PaymentId = "20150"
        });

        Assert.Equal(HttpMethod.Post, handler.Method);
        Assert.Equal("https://example.test/v2/AlfaPay/link/get", handler.RequestUri?.ToString());
        Assert.Equal("https://payment-app.com", response.Params?.RedirectUrl);

        using var document = JsonDocument.Parse(handler.Body!);
        var root = document.RootElement;

        Assert.Equal("TestB", root.GetProperty("TerminalKey").GetString());
        Assert.Equal("20150", root.GetProperty("PaymentId").GetString());
        Assert.Equal(
            "03acc0a77d6e870f402a1038c1ca5d8b4a985fe76f08016a869f10f2382bd7a9",
            root.GetProperty("Token").GetString());
    }

    [Fact]
    public async Task SendClosingReceiptAsync_Ffd12_PostsSignedRequestToCashboxEndpoint()
    {
        using var handler = new RecordingHandler("""
            {
              "Success": true,
              "ErrorCode": "0",
              "Message": "OK"
            }
            """);
        using var httpClient = new HttpClient(handler);
        var client = new TBankPaymentsClient(httpClient, new TBankPaymentsClientOptions
        {
            TerminalKey = "TestB",
            Password = "Dfsfh56dgKl",
            BaseAddress = new Uri("https://example.test/v2/")
        });

        var response = await client.SendClosingReceiptAsync(new TBankSendClosingReceiptFfd12Request
        {
            PaymentId = "20150",
            Receipt = new TBankReceiptFfd12
            {
                Taxation = "osn",
                Email = "a@test.ru",
                Items =
                [
                    new TBankReceiptItemFfd12
                    {
                        Name = "Товар 1",
                        Price = 10000,
                        Quantity = 1m,
                        Amount = 10000,
                        Tax = "vat10",
                        MeasurementUnit = "шт",
                        SectoralItemProps =
                        [
                            new TBankReceiptSectoralProps { FederalId = "001", Date = "21.11.2020", Number = "123/43", Value = "test" }
                        ]
                    }
                ]
            }
        });

        // The cashbox endpoint is at the host root, not under /v2/.
        Assert.Equal(HttpMethod.Post, handler.Method);
        Assert.Equal("https://example.test/cashbox/SendClosingReceipt", handler.RequestUri?.ToString());
        Assert.True(response.Success);

        using var document = JsonDocument.Parse(handler.Body!);
        var root = document.RootElement;

        Assert.Equal("TestB", root.GetProperty("TerminalKey").GetString());
        Assert.Equal("20150", root.GetProperty("PaymentId").GetString());

        var item = root.GetProperty("Receipt").GetProperty("Items")[0];
        Assert.Equal("Товар 1", item.GetProperty("Name").GetString());
        Assert.Equal(10000, item.GetProperty("Price").GetInt64());
        // FFD 1.2: SectoralItemProps is an array.
        Assert.Equal(JsonValueKind.Array, item.GetProperty("SectoralItemProps").ValueKind);

        // Receipt is an object, so it is excluded from the signing token.
        Assert.Equal(
            "03acc0a77d6e870f402a1038c1ca5d8b4a985fe76f08016a869f10f2382bd7a9",
            root.GetProperty("Token").GetString());
    }

    [Fact]
    public async Task SendClosingReceiptAsync_Ffd105_SerializesObjectSectoralPropsAndCamelCaseCheckProps()
    {
        using var handler = new RecordingHandler("""
            {
              "Success": true,
              "ErrorCode": "0",
              "Message": "OK"
            }
            """);
        using var httpClient = new HttpClient(handler);
        var client = new TBankPaymentsClient(httpClient, new TBankPaymentsClientOptions
        {
            TerminalKey = "TestB",
            Password = "Dfsfh56dgKl",
            BaseAddress = new Uri("https://example.test/v2/")
        });

        var response = await client.SendClosingReceiptAsync(new TBankSendClosingReceiptFfd105Request
        {
            PaymentId = "20150",
            Receipt = new TBankReceiptFfd105
            {
                Taxation = "osn",
                AdditionalCheckProps = "bso-1",
                Items =
                [
                    new TBankReceiptItemFfd105
                    {
                        Name = "Товар 1",
                        Price = 10000,
                        Quantity = 1m,
                        Amount = 10000,
                        Tax = "vat10",
                        Ean13 = "0123456789012",
                        SectoralItemProps = new TBankReceiptSectoralProps { FederalId = "001" }
                    }
                ]
            }
        });

        Assert.Equal("https://example.test/cashbox/SendClosingReceipt", handler.RequestUri?.ToString());
        Assert.True(response.Success);

        using var document = JsonDocument.Parse(handler.Body!);
        var receipt = document.RootElement.GetProperty("Receipt");

        // FFD 1.05: additionalCheckProps is camelCase.
        Assert.Equal("bso-1", receipt.GetProperty("additionalCheckProps").GetString());

        var item = receipt.GetProperty("Items")[0];
        Assert.Equal("0123456789012", item.GetProperty("Ean13").GetString());
        // FFD 1.05: SectoralItemProps is a single object.
        Assert.Equal(JsonValueKind.Object, item.GetProperty("SectoralItemProps").ValueKind);
    }

    private static TBankPaymentsClient CreateCardClient(HttpClient httpClient) =>
        new(httpClient, new TBankPaymentsClientOptions
        {
            TerminalKey = "TestB",
            Password = "Dfsfh56dgKl",
            BaseAddress = new Uri("https://example.test/v2/")
        });

    [Fact]
    public async Task AddCustomerAsync_PostsSignedRequestToAddCustomerEndpoint()
    {
        using var handler = new RecordingHandler("""
            {"TerminalKey":"TestB","CustomerKey":"cust-1","ErrorCode":"0","Success":true}
            """);
        using var httpClient = new HttpClient(handler);
        var client = CreateCardClient(httpClient);

        var response = await client.AddCustomerAsync(new TBankAddCustomerRequest
        {
            CustomerKey = "cust-1",
            Email = "a@test.ru",
            Phone = "+79031234567"
        });

        Assert.Equal("https://example.test/v2/AddCustomer", handler.RequestUri?.ToString());
        Assert.True(response.Success);
        Assert.Equal("cust-1", response.CustomerKey);

        using var document = JsonDocument.Parse(handler.Body!);
        var root = document.RootElement;
        Assert.Equal("TestB", root.GetProperty("TerminalKey").GetString());
        Assert.Equal("cust-1", root.GetProperty("CustomerKey").GetString());
        Assert.Equal("a@test.ru", root.GetProperty("Email").GetString());
        Assert.Equal("+79031234567", root.GetProperty("Phone").GetString());
        Assert.Equal("d1fa98403c6a3cd77bdf0fcde9040c3b20a2925c90722450aca755d56e70d1b6", root.GetProperty("Token").GetString());
    }

    [Fact]
    public async Task GetCustomerAsync_PostsSignedRequestAndReadsEmailPhone()
    {
        using var handler = new RecordingHandler("""
            {"TerminalKey":"TestB","CustomerKey":"cust-1","ErrorCode":"0","Success":true,"Email":"a@test.ru","Phone":"+79031234567"}
            """);
        using var httpClient = new HttpClient(handler);
        var client = CreateCardClient(httpClient);

        var response = await client.GetCustomerAsync(new TBankGetCustomerRequest { CustomerKey = "cust-1" });

        Assert.Equal("https://example.test/v2/GetCustomer", handler.RequestUri?.ToString());
        Assert.Equal("a@test.ru", response.Email);
        Assert.Equal("+79031234567", response.Phone);

        using var document = JsonDocument.Parse(handler.Body!);
        Assert.Equal("252f5ba2745d2a81b802a378f5a31eb3e9361c424b1cb2a723f5dd0efd20da08", document.RootElement.GetProperty("Token").GetString());
    }

    [Fact]
    public async Task RemoveCustomerAsync_PostsSignedRequestToRemoveCustomerEndpoint()
    {
        using var handler = new RecordingHandler("""
            {"TerminalKey":"TestB","CustomerKey":"cust-1","ErrorCode":"0","Success":true}
            """);
        using var httpClient = new HttpClient(handler);
        var client = CreateCardClient(httpClient);

        var response = await client.RemoveCustomerAsync(new TBankRemoveCustomerRequest { CustomerKey = "cust-1" });

        Assert.Equal("https://example.test/v2/RemoveCustomer", handler.RequestUri?.ToString());
        Assert.True(response.Success);

        using var document = JsonDocument.Parse(handler.Body!);
        Assert.Equal("252f5ba2745d2a81b802a378f5a31eb3e9361c424b1cb2a723f5dd0efd20da08", document.RootElement.GetProperty("Token").GetString());
    }

    [Fact]
    public async Task AddCardAsync_PostsSignedRequestAndReturnsPaymentUrl()
    {
        using var handler = new RecordingHandler("""
            {"PaymentId":6155312072,"TerminalKey":"TestB","CustomerKey":"cust-1","RequestKey":"ed989549-d3be-4758-95c7-22647e03f9ec","ErrorCode":"0","Success":true,"PaymentURL":"https://securepayments.tinkoff.ru/addcard/82a31a62"}
            """);
        using var httpClient = new HttpClient(handler);
        var client = CreateCardClient(httpClient);

        var response = await client.AddCardAsync(new TBankAddCardRequest
        {
            CustomerKey = "cust-1",
            CheckType = "NO",
            ResidentState = true
        });

        Assert.Equal("https://example.test/v2/AddCard", handler.RequestUri?.ToString());
        Assert.Equal("6155312072", response.PaymentId);
        Assert.Equal("ed989549-d3be-4758-95c7-22647e03f9ec", response.RequestKey);
        Assert.StartsWith("https://securepayments.tinkoff.ru/addcard/", response.PaymentURL);

        using var document = JsonDocument.Parse(handler.Body!);
        var root = document.RootElement;
        Assert.Equal("NO", root.GetProperty("CheckType").GetString());
        Assert.True(root.GetProperty("ResidentState").GetBoolean());
        Assert.Equal("9cba680368cbc8cac29def4e45354611fdd0aa2f1314b88f3ce7ed0d0c96617b", root.GetProperty("Token").GetString());
    }

    [Fact]
    public async Task GetAddCardStateAsync_PostsSignedRequestAndReadsStatus()
    {
        using var handler = new RecordingHandler("""
            {"TerminalKey":"TestB","RequestKey":"req-1","Status":"COMPLETED","Success":true,"CardId":"156516516","RebillId":"134249124","ErrorCode":"0","CustomerKey":"cust-1"}
            """);
        using var httpClient = new HttpClient(handler);
        var client = CreateCardClient(httpClient);

        var response = await client.GetAddCardStateAsync(new TBankGetAddCardStateRequest { RequestKey = "req-1" });

        Assert.Equal("https://example.test/v2/GetAddCardState", handler.RequestUri?.ToString());
        Assert.Equal(TBankPaymentStatus.COMPLETED, response.Status);
        Assert.Equal("156516516", response.CardId);
        Assert.Equal("134249124", response.RebillId);

        using var document = JsonDocument.Parse(handler.Body!);
        Assert.Equal("a3670ca9ba35f44d1c3db5f37797a7f2be66a10a74b07fae4ead6057605854fd", document.RootElement.GetProperty("Token").GetString());
    }

    [Fact]
    public async Task GetCardListAsync_ReturnsCardArray()
    {
        using var handler = new RecordingHandler("""
            [
              {"CardId":"881900","Pan":"518223******0036","Status":"A","RebillId":"6155312073","CardType":2,"ExpDate":"1122"},
              {"CardId":"881901","Pan":"518223******0044","Status":"D","CardType":0,"ExpDate":"1223"}
            ]
            """);
        using var httpClient = new HttpClient(handler);
        var client = CreateCardClient(httpClient);

        var cards = await client.GetCardListAsync(new TBankGetCardListRequest { CustomerKey = "cust-1", SavedCard = true });

        Assert.Equal("https://example.test/v2/GetCardList", handler.RequestUri?.ToString());
        Assert.Equal(2, cards.Count);
        Assert.Equal("881900", cards[0].CardId);
        Assert.Equal("518223******0036", cards[0].Pan);
        Assert.Equal(TBankCardStatus.ACTIVE, cards[0].Status);
        Assert.Equal(2, cards[0].CardType);
        Assert.Equal(TBankCardStatus.DELETED, cards[1].Status);

        using var document = JsonDocument.Parse(handler.Body!);
        var root = document.RootElement;
        Assert.True(root.GetProperty("SavedCard").GetBoolean());
        Assert.Equal("48b725186e7ab43522093a014aa1c0b42bad62dd742fcff0854299f5ae7d2dd2", root.GetProperty("Token").GetString());
    }

    [Fact]
    public async Task GetCardListAsync_ThrowsApiExceptionWhenBodyIsErrorObject()
    {
        using var handler = new RecordingHandler("""
            {"ErrorCode":"7","Message":"Неверные параметры","Details":"Покупатель не найден"}
            """);
        using var httpClient = new HttpClient(handler);
        var client = CreateCardClient(httpClient);

        var exception = await Assert.ThrowsAsync<TBankAcquiringApiException>(
            () => client.GetCardListAsync(new TBankGetCardListRequest { CustomerKey = "cust-1" }));

        Assert.Equal("7", exception.ErrorCode);
        Assert.Equal("Неверные параметры", exception.ErrorMessage);
        Assert.Equal("Покупатель не найден", exception.Details);
    }

    [Fact]
    public async Task ChargeAsync_ThrowsValidationWhenSendEmailWithoutInfoEmail()
    {
        using var handler = new RecordingHandler("{}");
        using var httpClient = new HttpClient(handler);
        var client = CreateCardClient(httpClient);

        await Assert.ThrowsAsync<TBankAcquiringValidationException>(
            () => client.ChargeAsync(new TBankChargeRequest { PaymentId = "20150", RebillId = "r-1", SendEmail = true }));
    }

    [Fact]
    public async Task RemoveCardAsync_PostsSignedRequestAndReadsCardStatus()
    {
        using var handler = new RecordingHandler("""
            {"TerminalKey":"TestB","Status":"D","CustomerKey":"cust-1","CardId":"card-1","CardType":0,"Success":true,"ErrorCode":"0"}
            """);
        using var httpClient = new HttpClient(handler);
        var client = CreateCardClient(httpClient);

        var response = await client.RemoveCardAsync(new TBankRemoveCardRequest { CustomerKey = "cust-1", CardId = "card-1" });

        Assert.Equal("https://example.test/v2/RemoveCard", handler.RequestUri?.ToString());
        Assert.Equal(TBankCardStatus.DELETED, response.Status);
        Assert.Equal(0, response.CardType);

        using var document = JsonDocument.Parse(handler.Body!);
        var root = document.RootElement;
        Assert.Equal("card-1", root.GetProperty("CardId").GetString());
        Assert.Equal("ba49e2e907c50abe0f076ad92da63343e4b0a03e1cd6e42af02e9bad9b4d916d", root.GetProperty("Token").GetString());
    }

    [Fact]
    public async Task ChargeAsync_PostsSignedRequestToChargeEndpoint()
    {
        using var handler = new RecordingHandler("""
            {"TerminalKey":"TestB","Amount":100000,"OrderId":"21050","Success":true,"Status":"CONFIRMED","PaymentId":"13660","ErrorCode":"0"}
            """);
        using var httpClient = new HttpClient(handler);
        var client = CreateCardClient(httpClient);

        var response = await client.ChargeAsync(new TBankChargeRequest { PaymentId = "20150", RebillId = "rebill-1" });

        Assert.Equal("https://example.test/v2/Charge", handler.RequestUri?.ToString());
        Assert.Equal(TBankPaymentStatus.CONFIRMED, response.Status);
        Assert.Equal(TBankAmount.FromMinorUnits(100000), response.Amount);
        Assert.Equal("13660", response.PaymentId);

        using var document = JsonDocument.Parse(handler.Body!);
        var root = document.RootElement;
        Assert.Equal("20150", root.GetProperty("PaymentId").GetString());
        Assert.Equal("rebill-1", root.GetProperty("RebillId").GetString());
        Assert.Equal("7e08029d24e35b2d050635e93a8924aebbc249ae2162b3a06720281c4c2ffb0a", root.GetProperty("Token").GetString());
    }

    [Fact]
    public async Task Check3dsVersionAsync_PostsSignedRequestToCheck3dsVersionEndpoint()
    {
        using var handler = new RecordingHandler("""
            {"Success":true,"ErrorCode":"0","Version":"2.1.0","TdsServerTransID":"tds-1","ThreeDSMethodURL":"https://acs.test/method","PaymentSystem":"Visa"}
            """);
        using var httpClient = new HttpClient(handler);
        var client = CreateCardClient(httpClient);

        var response = await client.Check3dsVersionAsync(new TBankCheck3dsVersionRequest
        {
            PaymentId = "13660",
            CardData = "encrypted-card"
        });

        Assert.Equal("https://example.test/v2/Check3dsVersion", handler.RequestUri?.ToString());
        Assert.Equal("2.1.0", response.Version);
        Assert.Equal("tds-1", response.TdsServerTransID);
        Assert.Equal("https://acs.test/method", response.ThreeDSMethodURL);
        Assert.Equal("Visa", response.PaymentSystem);

        using var document = JsonDocument.Parse(handler.Body!);
        var root = document.RootElement;
        Assert.Equal("TestB", root.GetProperty("TerminalKey").GetString());
        Assert.Equal("13660", root.GetProperty("PaymentId").GetString());
        Assert.Equal("encrypted-card", root.GetProperty("CardData").GetString());
        Assert.Equal("706979be67c1849fae179f707001847f230bfaaf69e11a4ec50f7325de8fc4b0", root.GetProperty("Token").GetString());
    }

    [Fact]
    public async Task AttachCardAsync_PostsSignedRequestToAttachCardEndpoint()
    {
        using var handler = new RecordingHandler("""
            {"Success":true,"ErrorCode":"0","TerminalKey":"TestB","CustomerKey":"cust-1","RequestKey":"req-1","CardId":"card-1","Status":"COMPLETED","RebillId":"6155312073"}
            """);
        using var httpClient = new HttpClient(handler);
        var client = CreateCardClient(httpClient);

        var response = await client.AttachCardAsync(new TBankAttachCardRequest
        {
            RequestKey = "req-1",
            CardData = "encrypted-card"
        });

        Assert.Equal("https://example.test/v2/AttachCard", handler.RequestUri?.ToString());
        Assert.Equal("card-1", response.CardId);
        Assert.Equal(TBankPaymentStatus.COMPLETED, response.Status);
        Assert.Equal("6155312073", response.RebillId);

        using var document = JsonDocument.Parse(handler.Body!);
        var root = document.RootElement;
        Assert.Equal("TestB", root.GetProperty("TerminalKey").GetString());
        Assert.Equal("req-1", root.GetProperty("RequestKey").GetString());
        Assert.Equal("encrypted-card", root.GetProperty("CardData").GetString());
        Assert.Equal("02", root.GetProperty("deviceChannel").GetString());
        Assert.Equal("aef52fa348e53f2122c30e5f32420a79de3569ca1eadfd871efd59179cdadf36", root.GetProperty("Token").GetString());
    }

    [Fact]
    public async Task AttachCardAsync_ReadsThreeDsCheckingFields()
    {
        using var handler = new RecordingHandler("""
            {"Success":true,"ErrorCode":"0","TerminalKey":"TestB","RequestKey":"req-1","Status":"3DS_CHECKING","ACSUrl":"https://acs.test/auth","MD":"md-1","PaReq":"pareq-1"}
            """);
        using var httpClient = new HttpClient(handler);
        var client = CreateCardClient(httpClient);

        var response = await client.AttachCardAsync(new TBankAttachCardRequest
        {
            RequestKey = "req-1",
            CardData = "encrypted-card"
        });

        Assert.Equal(TBankPaymentStatus.THREE_DS_CHECKING, response.Status);
        Assert.Equal("https://acs.test/auth", response.ACSUrl);
        Assert.Equal("md-1", response.MD);
        Assert.Equal("pareq-1", response.PaReq);
    }

    [Fact]
    public async Task Submit3DSAuthorizationAsync_PostsFormEncodedSignedRequest()
    {
        using var handler = new RecordingHandler("""
            {"Success":true,"ErrorCode":"0","TerminalKey":"TestB","OrderId":"order-1","Status":"CONFIRMED","PaymentId":"13660"}
            """);
        using var httpClient = new HttpClient(handler);
        var client = CreateCardClient(httpClient);

        var response = await client.Submit3DSAuthorizationAsync(new TBankSubmit3DSAuthorizationRequest
        {
            MD = "md-value",
            PaRes = "pares-value",
            PaymentId = "13660"
        });

        Assert.Equal("https://example.test/v2/Submit3DSAuthorization", handler.RequestUri?.ToString());
        Assert.Equal("application/x-www-form-urlencoded", handler.ContentType);
        Assert.Equal(TBankPaymentStatus.CONFIRMED, response.Status);
        Assert.Equal("13660", response.PaymentId);

        var form = ParseForm(handler.Body!);
        Assert.Equal("md-value", form["MD"]);
        Assert.Equal("pares-value", form["PaRes"]);
        Assert.Equal("13660", form["PaymentId"]);
        Assert.Equal("TestB", form["TerminalKey"]);
        Assert.Equal("ff0dcde2bf0f1791a466a5c66ad4301959ba3871c4a2c6282baf6ee957fb933b", form["Token"]);
    }

    [Fact]
    public async Task Submit3DSAuthorizationV2Async_PostsFormEncodedSignedRequest()
    {
        using var handler = new RecordingHandler("""
            {"Success":true,"ErrorCode":"0","TerminalKey":"TestB","OrderId":"order-1","Status":"AUTHORIZED","PaymentId":"13660"}
            """);
        using var httpClient = new HttpClient(handler);
        var client = CreateCardClient(httpClient);

        var response = await client.Submit3DSAuthorizationV2Async(new TBankSubmit3DSAuthorizationV2Request
        {
            PaymentId = "13660"
        });

        Assert.Equal("https://example.test/v2/Submit3DSAuthorizationV2", handler.RequestUri?.ToString());
        Assert.Equal("application/x-www-form-urlencoded", handler.ContentType);
        Assert.Equal(TBankPaymentStatus.AUTHORIZED, response.Status);

        var form = ParseForm(handler.Body!);
        Assert.Equal("13660", form["PaymentId"]);
        Assert.Equal("TestB", form["TerminalKey"]);
        Assert.Equal("7187ad41e8bb6d4d2ce3a24aed7348030079b7e9c3a7207e99ac4eecfd8b2c36", form["Token"]);
    }

    [Fact]
    public async Task GetConfirmOperationAsync_PostsTerminalKeyOnlyTokenAndReadsNumericErrorCode()
    {
        using var handler = new RecordingHandler("""
            {"Success":true,"ErrorCode":0,"PaymentIdList":[{"Success":true,"ErrorCode":0,"PaymentId":13660},{"Success":false,"ErrorCode":7,"Message":"Not found","PaymentId":13661}]}
            """);
        using var httpClient = new HttpClient(handler);
        var client = CreateCardClient(httpClient);

        var response = await client.GetConfirmOperationAsync(new TBankGetConfirmOperationRequest
        {
            CallbackUrl = "https://merchant.test/confirm",
            PaymentIdList = [13660, 13661]
        });

        Assert.Equal("https://example.test/v2/getConfirmOperation", handler.RequestUri?.ToString());
        Assert.True(response.Success);
        Assert.Equal("0", response.ErrorCode);
        Assert.Equal(2, response.PaymentIdList.Count);
        Assert.Equal("13660", response.PaymentIdList[0].PaymentId);
        Assert.False(response.PaymentIdList[1].Success);
        Assert.Equal("7", response.PaymentIdList[1].ErrorCode);
        Assert.Equal("13661", response.PaymentIdList[1].PaymentId);

        using var document = JsonDocument.Parse(handler.Body!);
        var root = document.RootElement;
        Assert.Equal("TestB", root.GetProperty("TerminalKey").GetString());
        Assert.Equal("https://merchant.test/confirm", root.GetProperty("CallbackUrl").GetString());
        Assert.Equal(13660, root.GetProperty("PaymentIdList")[0].GetInt64());
        Assert.Equal("34d48df4308e9325eed6f5d3744c4379213a110183b203ed21a9d984cb965f8a", root.GetProperty("Token").GetString());
    }

    [Fact]
    public async Task GetConfirmOperationAsync_PostsEmailListDeliveryChannel()
    {
        using var handler = new RecordingHandler("""
            {"Success":true,"ErrorCode":0,"PaymentIdList":[{"Success":true,"ErrorCode":0,"PaymentId":13660}]}
            """);
        using var httpClient = new HttpClient(handler);
        var client = CreateCardClient(httpClient);

        var response = await client.GetConfirmOperationAsync(new TBankGetConfirmOperationRequest
        {
            EmailList = [new TBankConfirmOperationEmail { Email = "ops@merchant.test" }],
            PaymentIdList = [13660]
        });

        Assert.True(response.Success);

        using var document = JsonDocument.Parse(handler.Body!);
        var root = document.RootElement;
        Assert.False(root.TryGetProperty("CallbackUrl", out _));
        Assert.Equal("ops@merchant.test", root.GetProperty("EmailList")[0].GetProperty("Email").GetString());
        // Token is still TerminalKey + Password only (EmailList is a nested array, excluded from signing).
        Assert.Equal("34d48df4308e9325eed6f5d3744c4379213a110183b203ed21a9d984cb965f8a", root.GetProperty("Token").GetString());
    }

    [Fact]
    public async Task GetConfirmOperationAsync_ThrowsApiExceptionOnTopLevelFailureInStrictMode()
    {
        using var handler = new RecordingHandler("""
            {"Success":false,"ErrorCode":600,"Message":"Invalid token"}
            """);
        using var httpClient = new HttpClient(handler);
        var client = new TBankPaymentsClient(httpClient, new TBankPaymentsClientOptions
        {
            TerminalKey = "TestB",
            Password = "Dfsfh56dgKl",
            BaseAddress = new Uri("https://example.test/v2/"),
            ThrowOnTBankApiError = true
        });

        var exception = await Assert.ThrowsAsync<TBankAcquiringApiException>(() => client.GetConfirmOperationAsync(new TBankGetConfirmOperationRequest
        {
            CallbackUrl = "https://merchant.test/confirm",
            PaymentIdList = [13660]
        }));

        Assert.Equal("600", exception.ErrorCode);
        Assert.Equal("Invalid token", exception.ErrorMessage);
    }

    [Fact]
    public async Task GetConfirmOperationAsync_NormalizesNullPaymentIdListToEmpty()
    {
        using var handler = new RecordingHandler("""
            {"Success":false,"ErrorCode":600,"Message":"Invalid token","PaymentIdList":null}
            """);
        using var httpClient = new HttpClient(handler);
        var client = CreateCardClient(httpClient);

        var response = await client.GetConfirmOperationAsync(new TBankGetConfirmOperationRequest
        {
            CallbackUrl = "https://merchant.test/confirm",
            PaymentIdList = [13660]
        });

        Assert.False(response.Success);
        Assert.NotNull(response.PaymentIdList);
        Assert.Empty(response.PaymentIdList);
    }

    private static Dictionary<string, string> ParseForm(string body) =>
        body.Split('&').Select(pair => pair.Split('=', 2)).ToDictionary(
            parts => Uri.UnescapeDataString(parts[0]),
            parts => Uri.UnescapeDataString(parts.Length > 1 ? parts[1] : string.Empty));

    private sealed class RecordingHandler(string responseBody, HttpStatusCode statusCode = HttpStatusCode.OK) : HttpMessageHandler
    {
        public Dictionary<string, string> ResponseHeaders { get; } = [];

        public Dictionary<string, string> ContentHeaders { get; } = [];

        public Uri? RequestUri { get; private set; }

        public HttpMethod? Method { get; private set; }

        public string? Body { get; private set; }

        public string? UserAgent { get; private set; }

        public string? Accept { get; private set; }

        public string? ContentType { get; private set; }

        protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            RequestUri = request.RequestUri;
            Method = request.Method;
            UserAgent = request.Headers.UserAgent.ToString();
            Accept = request.Headers.Accept.ToString();
            ContentType = request.Content?.Headers.ContentType?.MediaType;
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
