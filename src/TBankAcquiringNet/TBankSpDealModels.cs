namespace TBankAcquiringNet;

/// <summary>
/// Типы безопасной сделки для <see cref="TBankCreateSpDealRequest.SpDealType"/> и
/// <see cref="TBankInitPaymentRequest.CreateDealWithType"/>.
/// </summary>
public static class TBankSpDealTypes
{
    /// <summary>
    /// Накопительная сделка.
    /// </summary>
    /// <remarks>
    /// Единственное значение, которое банк принимает. На <c>MULTISPLIT</c> отвечает
    /// <c>256</c> «Указан некорректный тип безопасной сделки» — проверено на живом терминале.
    /// </remarks>
    public const string Accumulation = "NN";
}

/// <summary>
/// Запрос createSpDeal: открывает сделку, не привязывая её к платежу.
/// </summary>
/// <remarks>
/// Метод multisplit-терминала. Сделку можно открыть и платежом — <c>Init</c> с
/// <see cref="TBankInitPaymentRequest.CreateDealWithType"/>, — но тогда её идентификатор приходит
/// только в нотификации, и до первой нотификации площадка не знает, куда положила деньги. Этот метод
/// возвращает идентификатор сразу, поэтому сделку можно открыть заранее и дальше присоединять к ней
/// платежи через <see cref="TBankInitPaymentRequest.SpAccumulationId"/>.
/// </remarks>
public sealed record TBankCreateSpDealRequest
{
    /// <summary>Ключ терминала. Обычно заполняется клиентом автоматически.</summary>
    public string? TerminalKey { get; init; }

    /// <summary>Тип безопасной сделки.</summary>
    /// <remarks>См. <see cref="TBankSpDealTypes"/>: у банка это <c>NN</c> и ничего больше.</remarks>
    public string SpDealType { get; init; } = TBankSpDealTypes.Accumulation;

    /// <summary>Подпись запроса. Обычно генерируется клиентом автоматически.</summary>
    public string? Token { get; init; }
}

/// <summary>
/// Ответ метода createSpDeal.
/// </summary>
public sealed record TBankCreateSpDealResponse : TBankResponse
{
    /// <summary>Идентификатор открытой сделки.</summary>
    /// <remarks>
    /// То же поле, которым банк сообщает о сделке в нотификации
    /// (<see cref="TBankPaymentNotification.SpAccumulationId"/>), и то же значение, которое потом
    /// передают в <see cref="TBankInitPaymentRequest.SpAccumulationId"/> или
    /// <see cref="TBankInitPaymentRequest.DealId"/>.
    /// <para>
    /// Пример: <c>91954170</c>.
    /// </para>
    /// </remarks>
    public string? SpAccumulationId { get; init; }
}

/// <summary>
/// Запрос closeSpDeal: закрывает сделку, по которой были операции.
/// </summary>
/// <remarks>
/// Метод multisplit-терминала, и единственный необратимый из них.
/// <para>
/// <strong>Остаток на балансе сделки в момент закрытия целиком уходит площадке</strong> — и это
/// касается не только её вознаграждения: деньги Продавца, по которым выплата ещё не ушла, попадут
/// туда же. Закрывать сделку следует только тогда, когда всё, что причиталось Продавцам, уже
/// выплачено; иначе площадка получит чужие деньги и останется должна их вне расчётов банка.
/// </para>
/// <para>
/// То же происходит при <c>FinalPayout = true</c> в выплате: сделка закрывается, остаток достаётся
/// площадке. Отдельный вызов нужен, когда закрыть сделку надо без выплаты — например, если всё по
/// ней вернули Покупателям.
/// </para>
/// <para>
/// Выполняется на том терминале, на котором сделка была открыта.
/// </para>
/// </remarks>
public sealed record TBankCloseSpDealRequest
{
    /// <summary>Ключ терминала. Обычно заполняется клиентом автоматически.</summary>
    public string? TerminalKey { get; init; }

    /// <summary>Идентификатор закрываемой сделки.</summary>
    public required string SpAccumulationId { get; init; }

    /// <summary>Подпись запроса. Обычно генерируется клиентом автоматически.</summary>
    public string? Token { get; init; }
}

/// <summary>
/// Ответ метода closeSpDeal.
/// </summary>
public sealed record TBankCloseSpDealResponse : TBankResponse;
