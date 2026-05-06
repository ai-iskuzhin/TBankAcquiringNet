namespace TBankAcquiringNet.Payments;

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
