namespace TBankAcquiringNet.Payments;

/// <summary>
/// Среда T-Bank acquiring, в которую отправляются запросы.
/// </summary>
/// <remarks>
/// Базовый URL можно переопределить через <see cref="TBankPaymentsClientOptions.BaseAddress"/>,
/// если тестовый терминал обслуживается через боевой домен T-Bank.
/// </remarks>
public enum TBankAcquiringEnvironment
{
    /// <summary>Тестовая среда.</summary>
    /// <remarks>По умолчанию соответствует https://rest-api-test.tinkoff.ru/v2/.</remarks>
    Test,

    /// <summary>Боевая среда.</summary>
    /// <remarks>По умолчанию соответствует https://securepay.tinkoff.ru/v2/.</remarks>
    Production
}
