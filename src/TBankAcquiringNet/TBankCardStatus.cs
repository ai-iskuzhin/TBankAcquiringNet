namespace TBankAcquiringNet;

/// <summary>
/// Статус привязанной карты покупателя (метод GetCardList/RemoveCard).
/// </summary>
public enum TBankCardStatus
{
    /// <summary>Значение по умолчанию (статус не задан). API это значение не возвращает.</summary>
    UNKNOWN,

    /// <summary>Активная карта (A).</summary>
    ACTIVE,

    /// <summary>Удаленная карта (D).</summary>
    DELETED
}
