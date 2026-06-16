namespace TBankAcquiringNet;

/// <summary>
/// Денежная сумма в минимальных единицах валюты, например в копейках для RUB.
/// </summary>
/// <param name="MinorUnits">Сумма в минимальных единицах валюты.</param>
/// <example>
/// <code>
/// var amount = TBankAmount.FromMinorUnits(15000); // 150.00 RUB
/// </code>
/// </example>
public readonly record struct TBankAmount(long MinorUnits)
{
    /// <summary>
    /// Создает сумму из минимальных единиц валюты.
    /// </summary>
    /// <param name="minorUnits">Сумма в минимальных единицах валюты.</param>
    /// <returns>Экземпляр <see cref="TBankAmount"/>.</returns>
    public static TBankAmount FromMinorUnits(long minorUnits) => new(minorUnits);
}
