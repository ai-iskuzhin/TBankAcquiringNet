using System.Text.Json;

namespace TBankAcquiringNet;

/// <summary>
/// Проверка HTTP-нотификаций T-Bank.
/// </summary>
/// <example>
/// <code>
/// var validation = TBankPaymentNotificationValidator.ValidateToken(notification, password);
/// if (validation != TBankPaymentNotificationValidationResult.Valid)
/// {
///     return Results.BadRequest();
/// }
///
/// return Results.Text(TBankPaymentNotificationValidator.SuccessResponseBody);
/// </code>
/// </example>
public static class TBankPaymentNotificationValidator
{
    /// <summary>Тело ответа, которое нужно вернуть T-Bank после успешной обработки нотификации.</summary>
    public const string SuccessResponseBody = "OK";

    /// <summary>
    /// Разбирает тело нотификации и проверяет подпись за один проход.
    /// </summary>
    /// <remarks>
    /// Предпочтительный способ: подпись считается по полям так, как их прислал банк, поэтому она
    /// сходится и тогда, когда модель чего-то не знает — нового поля, незнакомого статуса. Поля,
    /// которых нет в модели, доступны в <see cref="TBankPaymentNotification.AdditionalFields"/>,
    /// а провод целиком — в <see cref="TBankPaymentNotification.RawFields"/>.
    /// </remarks>
    /// <param name="requestBody">Тело HTTP-запроса нотификации.</param>
    /// <param name="password">Пароль терминала.</param>
    /// <param name="notification">Разобранная нотификация, если тело — объект JSON.</param>
    /// <returns>Результат проверки подписи.</returns>
    public static TBankPaymentNotificationValidationResult TryRead(
        string requestBody,
        string password,
        out TBankPaymentNotification? notification)
    {
        notification = null;

        if (string.IsNullOrWhiteSpace(requestBody))
        {
            return TBankPaymentNotificationValidationResult.MalformedBody;
        }

        JsonDocument document;
        try
        {
            document = JsonDocument.Parse(requestBody);
        }
        catch (JsonException)
        {
            return TBankPaymentNotificationValidationResult.MalformedBody;
        }

        using (document)
        {
            if (document.RootElement.ValueKind != JsonValueKind.Object)
            {
                return TBankPaymentNotificationValidationResult.MalformedBody;
            }

            var rawFields = new Dictionary<string, string>(StringComparer.Ordinal);
            string? token = null;
            string? statusRaw = null;

            foreach (var property in document.RootElement.EnumerateObject())
            {
                // Вложенные объекты и массивы банк в подпись не берёт.
                if (property.Value.ValueKind is JsonValueKind.Object or JsonValueKind.Array)
                {
                    continue;
                }

                var value = FormatScalar(property.Value);

                if (string.Equals(property.Name, "Token", StringComparison.Ordinal))
                {
                    token = value;
                    continue;
                }

                if (string.Equals(property.Name, "Status", StringComparison.Ordinal))
                {
                    statusRaw = value;
                }

                rawFields[property.Name] = value;
            }

            // Отсутствие подписи — это отсутствие подписи, а не испорченное тело: Token в модели
            // обязателен, и разбор упал бы с невнятным MalformedBody.
            if (string.IsNullOrWhiteSpace(token))
            {
                return TBankPaymentNotificationValidationResult.MissingToken;
            }

            try
            {
                notification = JsonSerializer.Deserialize<TBankPaymentNotification>(requestBody);
            }
            catch (JsonException)
            {
                return TBankPaymentNotificationValidationResult.MalformedBody;
            }

            if (notification is null)
            {
                return TBankPaymentNotificationValidationResult.MalformedBody;
            }

            notification = notification with
            {
                Token = token ?? string.Empty,
                StatusRaw = statusRaw,
                RawFields = rawFields
            };
        }

        return ValidateToken(notification, password);
    }

    /// <summary>
    /// Значение так, как его подписал банк: булево — строчными, число — как в теле.
    /// </summary>
    private static string FormatScalar(JsonElement element)
    {
        return element.ValueKind switch
        {
            JsonValueKind.String => element.GetString() ?? string.Empty,
            JsonValueKind.True => "true",
            JsonValueKind.False => "false",
            JsonValueKind.Null => string.Empty,
            _ => element.GetRawText()
        };
    }

    /// <summary>
    /// Проверяет Token входящей нотификации.
    /// </summary>
    /// <param name="notification">HTTP-нотификация T-Bank.</param>
    /// <param name="password">Пароль терминала.</param>
    /// <returns>Результат проверки подписи.</returns>
    public static TBankPaymentNotificationValidationResult ValidateToken(
        TBankPaymentNotification notification,
        string password)
    {
        ArgumentNullException.ThrowIfNull(notification);

        if (string.IsNullOrWhiteSpace(notification.Token))
        {
            return TBankPaymentNotificationValidationResult.MissingToken;
        }

        return TBankToken.Verify(notification, password, notification.Token)
            ? TBankPaymentNotificationValidationResult.Valid
            : TBankPaymentNotificationValidationResult.InvalidToken;
    }
}
