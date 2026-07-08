using System.Text.Json.Serialization;

namespace TBankAcquiringNet;

// Фискальные поля-перечисления (Tax, PaymentMethod, PaymentObject, AgentSign и т.п.) заданы строками:
// их наборы значений зависят от версии ФФД и настроек кассы. T-Bank поддерживает две версии формата
// чека (oneOf Receipt_FFD_12 / Receipt_FFD_105), которые отличаются структурно, поэтому для каждой
// версии предусмотрен отдельный тип: TBankReceiptFfd12 и TBankReceiptFfd105.

/// <summary>
/// Данные фискального чека формата ФФД 1.2.
/// </summary>
public sealed record TBankReceiptFfd12
{
    /// <summary>Данные покупателя.</summary>
    public TBankReceiptClientInfo? ClientInfo { get; init; }

    /// <summary>Система налогообложения (ФФД 1055): osn, usn_income, usn_income_outcome, esn, patent.</summary>
    public required string Taxation { get; init; }

    /// <summary>Электронная почта покупателя. Обязательна, если не передан Phone.</summary>
    public string? Email { get; init; }

    /// <summary>Телефон покупателя в формате +{Ц}. Обязателен, если не передан Email.</summary>
    public string? Phone { get; init; }

    /// <summary>Идентификатор покупателя (ФФД 1227).</summary>
    public string? Customer { get; init; }

    /// <summary>ИНН покупателя (ФФД 1228).</summary>
    public string? CustomerInn { get; init; }

    /// <summary>Позиции чека (не больше 100).</summary>
    public required IReadOnlyList<TBankReceiptItemFfd12> Items { get; init; }

    /// <summary>Детали оплаты по видам.</summary>
    public TBankReceiptPayments? Payments { get; init; }

    /// <summary>Операционный реквизит чека (ФФД 1270).</summary>
    public TBankReceiptOperatingCheckProps? OperatingCheckProps { get; init; }

    /// <summary>Отраслевые реквизиты чека (ФФД 1261).</summary>
    public IReadOnlyList<TBankReceiptSectoralProps>? SectoralCheckProps { get; init; }

    /// <summary>Дополнительный реквизит пользователя (ФФД 1084).</summary>
    public TBankReceiptAddUserProp? AddUserProp { get; init; }

    /// <summary>Дополнительный реквизит чека БСО (ФФД 1192).</summary>
    public string? AdditionalCheckProps { get; init; }
}

/// <summary>
/// Позиция чека формата ФФД 1.2.
/// </summary>
public sealed record TBankReceiptItemFfd12
{
    /// <summary>Наименование товара (ФФД 1030).</summary>
    public required string Name { get; init; }

    /// <summary>Цена в копейках (ФФД 1079).</summary>
    public long Price { get; init; }

    /// <summary>Количество или вес товара (ФФД 1023).</summary>
    public decimal Quantity { get; init; }

    /// <summary>Стоимость товара в копейках, произведение Quantity и Price (ФФД 1043).</summary>
    public long Amount { get; init; }

    /// <summary>Ставка НДС (ФФД 1199): none, vat0, vat5, vat7, vat10, vat22, vat105, vat107, vat110, vat122.</summary>
    public required string Tax { get; init; }

    /// <summary>Признак способа расчета (ФФД 1214). По умолчанию full_payment.</summary>
    public string? PaymentMethod { get; init; }

    /// <summary>Признак предмета расчета (ФФД 1212).</summary>
    public string? PaymentObject { get; init; }

    /// <summary>Данные агента. Обязательны при агентской схеме.</summary>
    public TBankReceiptAgentData? AgentData { get; init; }

    /// <summary>Данные поставщика платежного агента.</summary>
    public TBankReceiptSupplierInfo? SupplierInfo { get; init; }

    /// <summary>Дополнительный реквизит предмета расчета (ФФД 1191).</summary>
    public string? UserData { get; init; }

    /// <summary>Сумма акциза (ФФД 1229).</summary>
    public string? Excise { get; init; }

    /// <summary>Цифровой код страны происхождения товара (ФФД 1230).</summary>
    public string? CountryCode { get; init; }

    /// <summary>Номер таможенной декларации (ФФД 1231).</summary>
    public string? DeclarationNumber { get; init; }

    /// <summary>Единица измерения (ФФД 2108). Обязательна для ФФД 1.2.</summary>
    public string? MeasurementUnit { get; init; }

    /// <summary>Режим обработки кода маркировки (ФФД 2102).</summary>
    public string? MarkProcessingMode { get; init; }

    /// <summary>Код маркировки товара (ФФД 1163).</summary>
    public TBankReceiptMarkCode? MarkCode { get; init; }

    /// <summary>Дробное количество маркированного товара (ФФД 1291).</summary>
    public TBankReceiptMarkQuantity? MarkQuantity { get; init; }

    /// <summary>Отраслевые реквизиты предмета расчета (ФФД 1260).</summary>
    public IReadOnlyList<TBankReceiptSectoralProps>? SectoralItemProps { get; init; }
}

/// <summary>
/// Данные фискального чека формата ФФД 1.05.
/// </summary>
public sealed record TBankReceiptFfd105
{
    /// <summary>Данные покупателя.</summary>
    public TBankReceiptClientInfo? ClientInfo { get; init; }

    /// <summary>Система налогообложения (ФФД 1055): osn, usn_income, usn_income_outcome, esn, patent.</summary>
    public required string Taxation { get; init; }

    /// <summary>Электронная почта покупателя. Обязательна, если не передан Phone.</summary>
    public string? Email { get; init; }

    /// <summary>Телефон покупателя в формате +{Ц}. Обязателен, если не передан Email.</summary>
    public string? Phone { get; init; }

    /// <summary>Идентификатор покупателя.</summary>
    public string? Customer { get; init; }

    /// <summary>ИНН покупателя.</summary>
    public string? CustomerInn { get; init; }

    /// <summary>Позиции чека (не больше 100).</summary>
    public required IReadOnlyList<TBankReceiptItemFfd105> Items { get; init; }

    /// <summary>Детали оплаты по видам.</summary>
    public TBankReceiptPayments? Payments { get; init; }

    /// <summary>Дополнительный реквизит пользователя (ФФД 1084).</summary>
    public TBankReceiptAddUserProp? AddUserProp { get; init; }

    /// <summary>Дополнительный реквизит чека БСО (ФФД 1192).</summary>
    [JsonPropertyName("additionalCheckProps")]
    public string? AdditionalCheckProps { get; init; }
}

/// <summary>
/// Позиция чека формата ФФД 1.05.
/// </summary>
public sealed record TBankReceiptItemFfd105
{
    /// <summary>Наименование товара (ФФД 1030).</summary>
    public required string Name { get; init; }

    /// <summary>Цена в копейках (ФФД 1078).</summary>
    public long Price { get; init; }

    /// <summary>Количество или вес товара (ФФД 1023).</summary>
    public decimal Quantity { get; init; }

    /// <summary>Стоимость товара в копейках, произведение Quantity и Price (ФФД 1043).</summary>
    public long Amount { get; init; }

    /// <summary>Ставка НДС (ФФД 1199): none, vat0, vat5, vat7, vat10, vat22, vat105, vat107, vat110, vat122.</summary>
    public required string Tax { get; init; }

    /// <summary>Признак способа расчета (ФФД 1214). По умолчанию full_payment.</summary>
    public string? PaymentMethod { get; init; }

    /// <summary>Признак предмета расчета (ФФД 1212). По умолчанию commodity.</summary>
    public string? PaymentObject { get; init; }

    /// <summary>Штрихкод (ФФД 1162).</summary>
    public string? Ean13 { get; init; }

    /// <summary>Код магазина.</summary>
    public string? ShopCode { get; init; }

    /// <summary>Данные агента. Обязательны при агентской схеме.</summary>
    public TBankReceiptAgentData? AgentData { get; init; }

    /// <summary>Данные поставщика платежного агента.</summary>
    public TBankReceiptSupplierInfo? SupplierInfo { get; init; }

    /// <summary>Отраслевой реквизит предмета расчета (ФФД 1260).</summary>
    public TBankReceiptSectoralProps? SectoralItemProps { get; init; }
}

/// <summary>
/// Данные покупателя в чеке.
/// </summary>
public sealed record TBankReceiptClientInfo
{
    /// <summary>Дата рождения.</summary>
    public string? Birthdate { get; init; }

    /// <summary>Гражданство.</summary>
    public string? Citizenship { get; init; }

    /// <summary>Код документа, удостоверяющего личность.</summary>
    public string? DocumentCode { get; init; }

    /// <summary>Данные документа, удостоверяющего личность.</summary>
    public string? DocumentData { get; init; }

    /// <summary>Адрес покупателя.</summary>
    public string? Address { get; init; }
}

/// <summary>
/// Данные агента платежного агента (ФФД 1223).
/// </summary>
public sealed record TBankReceiptAgentData
{
    /// <summary>Признак агента (ФФД 1222).</summary>
    public string? AgentSign { get; init; }

    /// <summary>Наименование операции (ФФД 1044).</summary>
    public string? OperationName { get; init; }

    /// <summary>Телефоны платежного агента (ФФД 1073).</summary>
    public IReadOnlyList<string>? Phones { get; init; }

    /// <summary>Телефоны оператора по приему платежей (ФФД 1074).</summary>
    public IReadOnlyList<string>? ReceiverPhones { get; init; }

    /// <summary>Телефоны оператора перевода (ФФД 1075).</summary>
    public IReadOnlyList<string>? TransferPhones { get; init; }

    /// <summary>Наименование оператора перевода (ФФД 1026).</summary>
    public string? OperatorName { get; init; }

    /// <summary>Адрес оператора перевода (ФФД 1005).</summary>
    public string? OperatorAddress { get; init; }

    /// <summary>ИНН оператора перевода (ФФД 1016).</summary>
    public string? OperatorInn { get; init; }
}

/// <summary>
/// Данные поставщика платежного агента (ФФД 1224).
/// </summary>
public sealed record TBankReceiptSupplierInfo
{
    /// <summary>Телефоны поставщика (ФФД 1171).</summary>
    public IReadOnlyList<string>? Phones { get; init; }

    /// <summary>Наименование поставщика (ФФД 1225).</summary>
    public string? Name { get; init; }

    /// <summary>ИНН поставщика (ФФД 1226).</summary>
    public string? Inn { get; init; }
}

/// <summary>
/// Код маркировки товара.
/// </summary>
public sealed record TBankReceiptMarkCode
{
    /// <summary>Тип штрихкода: UNKNOWN, EAN8, EAN13, ITF14, GS10, GS1M, SHORT, FUR, EGAIS20, EGAIS30, RAWCODE.</summary>
    public string? MarkCodeType { get; init; }

    /// <summary>Значение кода маркировки.</summary>
    public string? Value { get; init; }
}

/// <summary>
/// Дробное количество маркированного товара.
/// </summary>
public sealed record TBankReceiptMarkQuantity
{
    /// <summary>Числитель.</summary>
    public int Numerator { get; init; }

    /// <summary>Знаменатель.</summary>
    public int Denominator { get; init; }
}

/// <summary>
/// Отраслевой реквизит предмета расчета (ФФД 1260) или чека (ФФД 1261).
/// </summary>
public sealed record TBankReceiptSectoralProps
{
    /// <summary>Идентификатор ФОИВ (ФФД 1262).</summary>
    public string? FederalId { get; init; }

    /// <summary>Дата нормативного акта ФОИВ в формате ДД.ММ.ГГГГ (ФФД 1263).</summary>
    public string? Date { get; init; }

    /// <summary>Номер нормативного акта ФОИВ (ФФД 1264).</summary>
    public string? Number { get; init; }

    /// <summary>Состав значений отраслевого реквизита (ФФД 1265).</summary>
    public string? Value { get; init; }
}

/// <summary>
/// Операционный реквизит чека (ФФД 1270).
/// </summary>
public sealed record TBankReceiptOperatingCheckProps
{
    /// <summary>Идентификатор операции.</summary>
    public string? Name { get; init; }

    /// <summary>Данные операции.</summary>
    public string? Value { get; init; }

    /// <summary>Дата и время операции в формате dd.mm.yyyy HH:MM:SS.</summary>
    public string? Timestamp { get; init; }
}

/// <summary>
/// Детали оплаты чека по видам (суммы в копейках).
/// </summary>
public sealed record TBankReceiptPayments
{
    /// <summary>Вид оплаты «Наличные» (ФФД 1031).</summary>
    public long? Cash { get; init; }

    /// <summary>Вид оплаты «Безналичный» (ФФД 1081).</summary>
    public long? Electronic { get; init; }

    /// <summary>Вид оплаты «Предварительная оплата (Аванс)» (ФФД 1215).</summary>
    public long? AdvancePayment { get; init; }

    /// <summary>Вид оплаты «Постоплата (Кредит)» (ФФД 1216).</summary>
    public long? Credit { get; init; }

    /// <summary>Вид оплаты «Иная форма оплаты» (ФФД 1217).</summary>
    public long? Provision { get; init; }
}

/// <summary>
/// Дополнительный реквизит пользователя (ФФД 1084).
/// </summary>
public sealed record TBankReceiptAddUserProp
{
    /// <summary>Наименование реквизита (ФФД 1085).</summary>
    public required string Name { get; init; }

    /// <summary>Значение реквизита (ФФД 1086).</summary>
    public required string Value { get; init; }
}
