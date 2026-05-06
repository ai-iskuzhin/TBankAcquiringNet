namespace TBankAcquiringNet.Payments;

/// <summary>
/// Известные коды ошибок T-Bank payments/QR из локальной документации SDK.
/// </summary>
/// <remarks>
/// Каталог намеренно небольшой: неизвестные коды нужно читать из response.ErrorCode и не считать ошибкой SDK.
/// </remarks>
/// <example>
/// <code>
/// if (response.ErrorCode == TBankPaymentErrorCodes.InsufficientFunds)
/// {
///     // Недостаточно средств.
/// }
/// </code>
/// </example>
public static class TBankPaymentErrorCodes
{
    /// <summary>Операция выполнена успешно.</summary>
    public const string Success = "0";

    /// <summary>Операция по иностранной карте недоступна.</summary>
    public const string ForeignCardUnavailable = "76";

    /// <summary>Недостаточно средств.</summary>
    public const string InsufficientFunds = "1051";

    /// <summary>Оплата через QrPay недоступна.</summary>
    public const string QrPayUnavailable = "3001";

    /// <summary>Привязка счета не найдена.</summary>
    public const string AccountBindingNotFound = "3012";

    /// <summary>Рекуррентные платежи недоступны.</summary>
    public const string RecurrentPaymentsUnavailable = "3013";

    /// <summary>Неверный статус AccountToken.</summary>
    public const string InvalidAccountTokenStatus = "3015";

    private static readonly IReadOnlyDictionary<string, string> Descriptions = new Dictionary<string, string>(StringComparer.Ordinal)
    {
        [Success] = "Операция выполнена успешно.",
        [ForeignCardUnavailable] = "Операция по иностранной карте недоступна.",
        [InsufficientFunds] = "Недостаточно средств.",
        [QrPayUnavailable] = "Оплата через QrPay недоступна.",
        [AccountBindingNotFound] = "Привязка счета не найдена.",
        [RecurrentPaymentsUnavailable] = "Рекуррентные платежи недоступны.",
        [InvalidAccountTokenStatus] = "Неверный статус AccountToken."
    };

    /// <summary>
    /// Возвращает true, если код означает успешный ответ T-Bank.
    /// </summary>
    /// <param name="errorCode">Код ошибки из ответа T-Bank.</param>
    /// <returns>true только для кода "0".</returns>
    public static bool IsSuccess(string? errorCode)
    {
        return string.Equals(errorCode, Success, StringComparison.Ordinal);
    }

    /// <summary>
    /// Пытается получить русское описание известного кода ошибки.
    /// </summary>
    /// <param name="errorCode">Код ошибки из ответа T-Bank.</param>
    /// <param name="description">Русское описание, если код известен SDK.</param>
    /// <returns>true, если код найден в локальном каталоге.</returns>
    public static bool TryGetDescription(string? errorCode, out string description)
    {
        if (errorCode is not null && Descriptions.TryGetValue(errorCode, out var knownDescription))
        {
            description = knownDescription;
            return true;
        }

        description = string.Empty;
        return false;
    }

    /// <summary>
    /// Возвращает русское описание известного кода ошибки или null для неизвестного кода.
    /// </summary>
    /// <param name="errorCode">Код ошибки из ответа T-Bank.</param>
    /// <returns>Русское описание или null.</returns>
    public static string? GetDescription(string? errorCode)
    {
        return TryGetDescription(errorCode, out var description) ? description : null;
    }
}
