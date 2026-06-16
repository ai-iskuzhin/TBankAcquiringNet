namespace TBankAcquiringNet;

/// <summary>
/// Статус привязки счета покупателя к магазину по СБП. Имена совпадают с проводными значениями API.
/// </summary>
public enum TBankAccountQrStatus
{
    /// <summary>Значение по умолчанию (статус не задан). API это значение не возвращает.</summary>
    UNKNOWN,

    /// <summary>Получен запрос на привязку счета (NEW).</summary>
    NEW,

    /// <summary>Запрос в обработке (PROCESSING).</summary>
    PROCESSING,

    /// <summary>Привязка счета успешна (ACTIVE).</summary>
    ACTIVE,

    /// <summary>Привязка счета неуспешна или деактивирована (INACTIVE).</summary>
    INACTIVE
}
