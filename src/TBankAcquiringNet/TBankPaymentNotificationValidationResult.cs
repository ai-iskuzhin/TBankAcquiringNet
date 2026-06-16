namespace TBankAcquiringNet;

/// <summary>
/// Результат проверки подписи HTTP-нотификации T-Bank.
/// </summary>
/// <remarks>
/// Используйте значение <see cref="Valid"/> как единственное основание доверять полям нотификации.
/// При любом другом результате обработчик должен отклонить или изолировать callback.
/// </remarks>
public enum TBankPaymentNotificationValidationResult
{
    /// <summary>Подпись корректна.</summary>
    /// <remarks>Можно продолжать бизнес-обработку нотификации.</remarks>
    Valid,

    /// <summary>В нотификации отсутствует значение Token.</summary>
    /// <remarks>Обычно означает некорректный или неполный callback.</remarks>
    MissingToken,

    /// <summary>Значение Token не совпадает с рассчитанной подписью.</summary>
    /// <remarks>Нотификацию нельзя считать доверенной.</remarks>
    InvalidToken
}
