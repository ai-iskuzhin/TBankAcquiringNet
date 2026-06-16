namespace TBankAcquiringNet;

/// <summary>
/// Тип проведения платежа (PayType) в методе Init. Если не задан, используется настройка терминала.
/// </summary>
public enum TBankPayType
{
    /// <summary>Одностадийная оплата (O).</summary>
    OneStage,

    /// <summary>Двухстадийная оплата (T).</summary>
    TwoStage
}
