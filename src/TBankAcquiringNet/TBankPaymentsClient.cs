using System.Net.Http.Json;
using System.Text.Json;
using System.Text.RegularExpressions;
using System.Text.Json.Serialization;

namespace TBankAcquiringNet;

/// <summary>
/// Клиент платежных методов T-Bank acquiring.
/// </summary>
public sealed class TBankPaymentsClient
{
    private static readonly JsonSerializerOptions JsonOptions = CreateJsonOptions();

    private readonly HttpClient httpClient;
    private readonly TBankPaymentsClientOptions options;

    /// <summary>
    /// Создает клиент платежного API T-Bank.
    /// </summary>
    public TBankPaymentsClient(HttpClient httpClient, TBankPaymentsClientOptions options)
    {
        ArgumentNullException.ThrowIfNull(httpClient);
        ArgumentNullException.ThrowIfNull(options);

        if (string.IsNullOrWhiteSpace(options.TerminalKey))
        {
            throw new ArgumentException("Terminal key must be configured.", nameof(options));
        }

        if (string.IsNullOrWhiteSpace(options.Password))
        {
            throw new ArgumentException("Password must be configured.", nameof(options));
        }

        this.httpClient = httpClient;
        this.options = options;
    }

    /// <summary>
    /// Инициирует платежную сессию методом Init.
    /// </summary>
    public Task<TBankInitPaymentResponse> InitAsync(
        TBankInitPaymentRequest request,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(request);
        TBankPaymentRequestValidator.Validate(request);

        var signedRequest = request with
        {
            TerminalKey = options.TerminalKey
        };

        signedRequest = signedRequest with
        {
            Token = CreateToken(signedRequest, request.Token)
        };

        return SendAsync<TBankInitPaymentRequest, TBankInitPaymentResponse>("Init", signedRequest, cancellationToken);
    }

    /// <summary>
    /// Возвращает состояние платежа методом GetState.
    /// </summary>
    public Task<TBankPaymentStateResponse> GetStateAsync(
        TBankPaymentStateRequest request,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(request);
        TBankPaymentRequestValidator.Validate(request);

        var signedRequest = request with
        {
            TerminalKey = options.TerminalKey
        };

        signedRequest = signedRequest with
        {
            Token = CreateToken(signedRequest, request.Token)
        };

        return SendAsync<TBankPaymentStateRequest, TBankPaymentStateResponse>("GetState", signedRequest, cancellationToken);
    }

    /// <summary>
    /// Возвращает состояние заказа и связанные платежи методом CheckOrder.
    /// </summary>
    public Task<TBankCheckOrderResponse> CheckOrderAsync(
        TBankCheckOrderRequest request,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(request);
        TBankPaymentRequestValidator.Validate(request);

        var signedRequest = request with
        {
            TerminalKey = options.TerminalKey
        };

        signedRequest = signedRequest with
        {
            Token = CreateToken(signedRequest, request.Token)
        };

        return SendAsync<TBankCheckOrderRequest, TBankCheckOrderResponse>("CheckOrder", signedRequest, cancellationToken);
    }

    /// <summary>
    /// Отменяет платеж или выполняет возврат методом Cancel.
    /// </summary>
    public Task<TBankCancelPaymentResponse> CancelAsync(
        TBankCancelPaymentRequest request,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(request);
        TBankPaymentRequestValidator.Validate(request);

        var signedRequest = request with
        {
            TerminalKey = options.TerminalKey
        };

        signedRequest = signedRequest with
        {
            Token = CreateToken(signedRequest, request.Token)
        };

        return SendAsync<TBankCancelPaymentRequest, TBankCancelPaymentResponse>("Cancel", signedRequest, cancellationToken);
    }

    /// <summary>
    /// Подтверждает авторизованный платеж методом Confirm.
    /// </summary>
    public Task<TBankConfirmPaymentResponse> ConfirmAsync(
        TBankConfirmPaymentRequest request,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(request);
        TBankPaymentRequestValidator.Validate(request);

        var signedRequest = request with
        {
            TerminalKey = options.TerminalKey
        };

        signedRequest = signedRequest with
        {
            Token = CreateToken(signedRequest, request.Token)
        };

        return SendAsync<TBankConfirmPaymentRequest, TBankConfirmPaymentResponse>("Confirm", signedRequest, cancellationToken);
    }

    /// <summary>
    /// Регистрирует QR и возвращает payload или изображение методом GetQr.
    /// </summary>
    public Task<TBankQrResponse> GetQrAsync(
        TBankQrRequest request,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(request);
        TBankPaymentRequestValidator.Validate(request);

        var signedRequest = request with
        {
            TerminalKey = options.TerminalKey
        };

        signedRequest = signedRequest with
        {
            Token = CreateToken(signedRequest, request.Token)
        };

        return SendAsync<TBankQrRequest, TBankQrResponse>("GetQr", signedRequest, cancellationToken);
    }

    /// <summary>
    /// Выполняет списание по привязанному QR-счету методом ChargeQr.
    /// </summary>
    public Task<TBankChargeQrResponse> ChargeQrAsync(
        TBankChargeQrRequest request,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(request);
        TBankPaymentRequestValidator.Validate(request);

        var signedRequest = request with
        {
            TerminalKey = options.TerminalKey
        };

        signedRequest = signedRequest with
        {
            Token = CreateToken(signedRequest, request.Token)
        };

        return SendAsync<TBankChargeQrRequest, TBankChargeQrResponse>("ChargeQr", signedRequest, cancellationToken);
    }

    /// <summary>
    /// Возвращает статус возврата платежа по СБП методом GetQrState.
    /// </summary>
    public Task<TBankQrStateResponse> GetQrStateAsync(
        TBankQrStateRequest request,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(request);
        TBankPaymentRequestValidator.Validate(request);

        var signedRequest = request with
        {
            TerminalKey = options.TerminalKey
        };

        signedRequest = signedRequest with
        {
            Token = CreateToken(signedRequest, request.Token)
        };

        return SendAsync<TBankQrStateRequest, TBankQrStateResponse>("GetQrState", signedRequest, cancellationToken);
    }

    /// <summary>
    /// Возвращает список банков-участников СБП методом GetQrBankList.
    /// </summary>
    public Task<TBankQrBankListResponse> GetQrBankListAsync(
        TBankQrBankListRequest request,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(request);
        TBankPaymentRequestValidator.Validate(request);

        var signedRequest = request with
        {
            TerminalKey = options.TerminalKey
        };

        signedRequest = signedRequest with
        {
            Token = CreateToken(signedRequest, request.Token)
        };

        return SendAsync<TBankQrBankListRequest, TBankQrBankListResponse>("GetQrBankList", signedRequest, cancellationToken);
    }

    /// <summary>
    /// Возвращает список привязанных к магазину счетов методом GetAccountQrList.
    /// </summary>
    public Task<TBankAccountQrListResponse> GetAccountQrListAsync(
        TBankAccountQrListRequest request,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(request);
        TBankPaymentRequestValidator.Validate(request);

        var signedRequest = request with
        {
            TerminalKey = options.TerminalKey
        };

        signedRequest = signedRequest with
        {
            Token = CreateToken(signedRequest, request.Token)
        };

        return SendAsync<TBankAccountQrListRequest, TBankAccountQrListResponse>("GetAccountQrList", signedRequest, cancellationToken);
    }

    /// <summary>
    /// Возвращает статус привязки счета к магазину методом GetAddAccountQrState.
    /// </summary>
    public Task<TBankAddAccountQrStateResponse> GetAddAccountQrStateAsync(
        TBankAddAccountQrStateRequest request,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(request);
        TBankPaymentRequestValidator.Validate(request);

        var signedRequest = request with
        {
            TerminalKey = options.TerminalKey
        };

        signedRequest = signedRequest with
        {
            Token = CreateToken(signedRequest, request.Token)
        };

        return SendAsync<TBankAddAccountQrStateRequest, TBankAddAccountQrStateResponse>("GetAddAccountQrState", signedRequest, cancellationToken);
    }

    /// <summary>
    /// Привязывает счет покупателя к магазину методом AddAccountQr.
    /// </summary>
    public Task<TBankAddAccountQrResponse> AddAccountQrAsync(
        TBankAddAccountQrRequest request,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(request);
        TBankPaymentRequestValidator.Validate(request);

        var signedRequest = request with
        {
            TerminalKey = options.TerminalKey
        };

        signedRequest = signedRequest with
        {
            Token = CreateToken(signedRequest, request.Token)
        };

        return SendAsync<TBankAddAccountQrRequest, TBankAddAccountQrResponse>("AddAccountQr", signedRequest, cancellationToken);
    }

    /// <summary>
    /// Возвращает список банков-участников QR для возврата методом QrMembersList.
    /// </summary>
    public Task<TBankQrMembersListResponse> QrMembersListAsync(
        TBankQrMembersListRequest request,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(request);
        TBankPaymentRequestValidator.Validate(request);

        var signedRequest = request with
        {
            TerminalKey = options.TerminalKey
        };

        signedRequest = signedRequest with
        {
            Token = CreateToken(signedRequest, request.Token)
        };

        return SendAsync<TBankQrMembersListRequest, TBankQrMembersListResponse>("QrMembersList", signedRequest, cancellationToken);
    }

    /// <summary>
    /// Создает тестовую платежную сессию СБП методом SbpPayTest. Доступно только на тестовом терминале.
    /// </summary>
    public Task<TBankSbpPayTestResponse> SbpPayTestAsync(
        TBankSbpPayTestRequest request,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(request);
        TBankPaymentRequestValidator.Validate(request);

        var signedRequest = request with
        {
            TerminalKey = options.TerminalKey
        };

        signedRequest = signedRequest with
        {
            Token = CreateToken(signedRequest, request.Token)
        };

        return SendAsync<TBankSbpPayTestRequest, TBankSbpPayTestResponse>("SbpPayTest", signedRequest, cancellationToken);
    }

    private string? CreateToken<TRequest>(TRequest request, string? suppliedToken)
    {
        if (!options.AutoGenerateToken)
        {
            return suppliedToken;
        }

        return TBankToken.Create(request, options.Password);
    }

    private async Task<TResponse> SendAsync<TRequest, TResponse>(
        string method,
        TRequest request,
        CancellationToken cancellationToken)
        where TResponse : TBankResponse
    {
        var endpoint = new Uri(options.ResolveBaseAddress(), method);
        HttpResponseMessage response;

        try
        {
            response = await httpClient.PostAsJsonAsync(endpoint, request, JsonOptions, cancellationToken)
                .ConfigureAwait(false);
        }
        catch (HttpRequestException exception)
        {
            throw new TBankAcquiringTransportException($"T-Bank {method} request failed before a response was received.", exception);
        }

        using (response)
        {
#if NETSTANDARD2_0
            var responseBody = await response.Content.ReadAsStringAsync()
                .ConfigureAwait(false);
#else
            var responseBody = await response.Content.ReadAsStringAsync(cancellationToken)
                .ConfigureAwait(false);
#endif

            var result = DeserializeResponse<TResponse>(method, response.StatusCode, responseBody);
            result = result with
            {
                Metadata = CreateResponseMetadata(response, responseBody, options.CaptureRawResponseBody)
            };

            if (options.ThrowOnTBankApiError && !result.Success)
            {
                throw new TBankAcquiringApiException(
                    $"T-Bank {method} returned ErrorCode '{result.ErrorCode}'.",
                    result.ErrorCode,
                    result.Message,
                    result.Details,
                    response.StatusCode);
            }

            return result;
        }
    }

    private static TBankAcquiringResponseMetadata CreateResponseMetadata(
        HttpResponseMessage response,
        string responseBody,
        bool captureRawResponseBody)
    {
        var headers = response.Headers
            .Concat(response.Content.Headers)
            .GroupBy(static header => header.Key, StringComparer.OrdinalIgnoreCase)
            .ToDictionary(
                static group => group.Key,
                static group => group.SelectMany(static header => header.Value).ToArray(),
                StringComparer.OrdinalIgnoreCase);

        return new TBankAcquiringResponseMetadata(
            response.StatusCode,
            headers,
            captureRawResponseBody ? responseBody : null);
    }

    private static TResponse DeserializeResponse<TResponse>(
        string method,
        System.Net.HttpStatusCode statusCode,
        string responseBody)
        where TResponse : TBankResponse
    {
        if (string.IsNullOrWhiteSpace(responseBody))
        {
            throw new TBankAcquiringProtocolException(
                $"T-Bank {method} response body was empty. HTTP {(int)statusCode} ({statusCode}).",
                statusCode);
        }

        try
        {
            return JsonSerializer.Deserialize<TResponse>(responseBody, JsonOptions)
                ?? throw new TBankAcquiringProtocolException(
                    $"T-Bank {method} response body was empty after deserialization.",
                    statusCode,
                    CreateBodyPreview(responseBody));
        }
        catch (JsonException exception)
        {
            var responseBodyPreview = CreateBodyPreview(responseBody);
            throw new TBankAcquiringProtocolException(
                $"T-Bank {method} response body was not valid JSON for the expected response model. HTTP {(int)statusCode} ({statusCode}). Response preview: {responseBodyPreview}",
                statusCode,
                responseBodyPreview,
                exception);
        }
    }

    private static string CreateBodyPreview(string responseBody)
    {
        var preview = RedactSensitiveFields(responseBody);
        const int maxLength = 512;

        return preview.Length <= maxLength ? preview : preview[..maxLength];
    }

    private static string RedactSensitiveFields(string value)
    {
        return Regex.Replace(
            value,
            "(\"(?:Token|Password|CardData|CVV|DigestValue|SignatureValue)\"\\s*:\\s*\")([^\"]*)(\")",
            "$1***REDACTED***$3",
            RegexOptions.IgnoreCase | RegexOptions.CultureInvariant);
    }

    private static JsonSerializerOptions CreateJsonOptions()
    {
        var jsonOptions = new JsonSerializerOptions(JsonSerializerDefaults.Web)
        {
            DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull,
            PropertyNamingPolicy = null
        };

        jsonOptions.Converters.Add(new JsonStringEnumConverter());
        jsonOptions.Converters.Add(new TBankAmountJsonConverter());
        jsonOptions.Converters.Add(new TBankPaymentStatusJsonConverter());
        jsonOptions.Converters.Add(new TBankQrDataTypeJsonConverter());

        return jsonOptions;
    }
}
