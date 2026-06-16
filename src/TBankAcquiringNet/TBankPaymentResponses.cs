using System.Text.Json.Serialization;

namespace TBankAcquiringNet;

/// <summary>
/// Базовый ответ T-Bank API.
/// </summary>
public abstract record TBankResponse
{
    /// <summary>Признак успешности ответа T-Bank.</summary>
    public bool Success { get; init; }

    /// <summary>Код ошибки T-Bank.</summary>
    public string? ErrorCode { get; init; }

    /// <summary>Краткое сообщение T-Bank.</summary>
    public string? Message { get; init; }

    /// <summary>Детали ошибки T-Bank.</summary>
    public string? Details { get; init; }

    /// <summary>HTTP-метаданные ответа.</summary>
    [JsonIgnore]
    public TBankAcquiringResponseMetadata? Metadata { get; init; }
}

/// <summary>
/// Ответ метода Init.
/// </summary>
public sealed record TBankInitPaymentResponse : TBankResponse
{
    /// <summary>Ключ терминала.</summary>
    public string? TerminalKey { get; init; }

    /// <summary>Сумма платежа.</summary>
    [JsonConverter(typeof(TBankAmountJsonConverter))]
    public TBankAmount? Amount { get; init; }

    /// <summary>Номер заказа на стороне площадки.</summary>
    public string? OrderId { get; init; }

    /// <summary>Идентификатор платежа в T-Bank.</summary>
    [JsonConverter(typeof(TBankStringJsonConverter))]
    public string? PaymentId { get; init; }

    /// <summary>URL платежной формы.</summary>
    public string? PaymentURL { get; init; }

    /// <summary>Статус платежа.</summary>
    [JsonConverter(typeof(TBankPaymentStatusJsonConverter))]
    public TBankPaymentStatus Status { get; init; }
}

/// <summary>
/// Ответ метода GetState.
/// </summary>
public sealed record TBankPaymentStateResponse : TBankResponse
{
    /// <summary>Ключ терминала.</summary>
    public string? TerminalKey { get; init; }

    /// <summary>Сумма операции.</summary>
    [JsonConverter(typeof(TBankAmountJsonConverter))]
    public TBankAmount? Amount { get; init; }

    /// <summary>Номер заказа на стороне площадки.</summary>
    public string? OrderId { get; init; }

    /// <summary>Идентификатор платежа в T-Bank.</summary>
    [JsonConverter(typeof(TBankStringJsonConverter))]
    public string? PaymentId { get; init; }

    /// <summary>Статус платежа.</summary>
    [JsonConverter(typeof(TBankPaymentStatusJsonConverter))]
    public TBankPaymentStatus Status { get; init; }

    /// <summary>Идентификатор привязанной карты.</summary>
    public string? CardId { get; init; }

    /// <summary>Маскированный номер карты или телефона.</summary>
    public string? Pan { get; init; }

    /// <summary>Срок действия карты.</summary>
    public string? ExpDate { get; init; }

    /// <summary>Идентификатор рекуррентного платежа.</summary>
    public string? RebillId { get; init; }

    /// <summary>Идентификатор привязки счета.</summary>
    public string? AccountToken { get; init; }
}

/// <summary>
/// Ответ метода CheckOrder.
/// </summary>
public sealed record TBankCheckOrderResponse : TBankResponse
{
    /// <summary>Ключ терминала.</summary>
    public string? TerminalKey { get; init; }

    /// <summary>Номер заказа на стороне площадки.</summary>
    public string? OrderId { get; init; }

    /// <summary>Платежи, связанные с заказом.</summary>
    public IReadOnlyList<TBankOrderPayment> Payments { get; init; } = [];
}

/// <summary>
/// Платеж в ответе CheckOrder.
/// </summary>
public sealed record TBankOrderPayment
{
    /// <summary>Идентификатор платежа в T-Bank.</summary>
    [JsonConverter(typeof(TBankStringJsonConverter))]
    public string? PaymentId { get; init; }

    /// <summary>Сумма операции.</summary>
    [JsonConverter(typeof(TBankAmountJsonConverter))]
    public TBankAmount? Amount { get; init; }

    /// <summary>Статус платежа.</summary>
    [JsonConverter(typeof(TBankPaymentStatusJsonConverter))]
    public TBankPaymentStatus Status { get; init; }

    /// <summary>RRN операции.</summary>
    [JsonPropertyName("Rrn")]
    [JsonConverter(typeof(TBankStringJsonConverter))]
    public string? RRN { get; init; }

    /// <summary>Признак успешности платежа.</summary>
    public bool Success { get; init; }

    /// <summary>Код ошибки платежа.</summary>
    [JsonConverter(typeof(TBankStringJsonConverter))]
    public string? ErrorCode { get; init; }

    /// <summary>Сообщение платежа.</summary>
    public string? Message { get; init; }
}

/// <summary>
/// Ответ метода Cancel.
/// </summary>
public sealed record TBankCancelPaymentResponse : TBankResponse
{
    /// <summary>Ключ терминала.</summary>
    public string? TerminalKey { get; init; }

    /// <summary>Идентификатор платежа в T-Bank.</summary>
    [JsonConverter(typeof(TBankStringJsonConverter))]
    public string? PaymentId { get; init; }

    /// <summary>Номер заказа на стороне площадки.</summary>
    public string? OrderId { get; init; }

    /// <summary>Статус платежа.</summary>
    [JsonConverter(typeof(TBankPaymentStatusJsonConverter))]
    public TBankPaymentStatus Status { get; init; }

    /// <summary>Исходная сумма до отмены или возврата.</summary>
    [JsonConverter(typeof(TBankAmountJsonConverter))]
    public TBankAmount? OriginalAmount { get; init; }

    /// <summary>Новая сумма после отмены или возврата.</summary>
    [JsonConverter(typeof(TBankAmountJsonConverter))]
    public TBankAmount? NewAmount { get; init; }

    /// <summary>Идентификатор операции на стороне мерчанта.</summary>
    public string? ExternalRequestId { get; init; }
}

/// <summary>
/// Ответ метода Confirm.
/// </summary>
public sealed record TBankConfirmPaymentResponse : TBankResponse
{
    /// <summary>Ключ терминала.</summary>
    public string? TerminalKey { get; init; }

    /// <summary>Идентификатор платежа в T-Bank.</summary>
    [JsonConverter(typeof(TBankStringJsonConverter))]
    public string? PaymentId { get; init; }

    /// <summary>Номер заказа на стороне площадки.</summary>
    public string? OrderId { get; init; }

    /// <summary>Статус платежа.</summary>
    [JsonConverter(typeof(TBankPaymentStatusJsonConverter))]
    public TBankPaymentStatus Status { get; init; }

    /// <summary>Сумма подтверждения.</summary>
    [JsonConverter(typeof(TBankAmountJsonConverter))]
    public TBankAmount? Amount { get; init; }
}

/// <summary>
/// Ответ метода GetQr.
/// </summary>
public sealed record TBankQrResponse : TBankResponse
{
    /// <summary>Ключ терминала.</summary>
    public string? TerminalKey { get; init; }

    /// <summary>Номер заказа на стороне площадки.</summary>
    public string? OrderId { get; init; }

    /// <summary>Идентификатор платежа в T-Bank.</summary>
    [JsonConverter(typeof(TBankStringJsonConverter))]
    public string? PaymentId { get; init; }

    /// <summary>QR payload или SVG-изображение.</summary>
    public string? Data { get; init; }
}

/// <summary>
/// Ответ метода GetQrState.
/// </summary>
public sealed record TBankQrStateResponse : TBankResponse
{
    /// <summary>Ключ терминала.</summary>
    public string? TerminalKey { get; init; }

    /// <summary>Номер заказа на стороне площадки.</summary>
    public string? OrderId { get; init; }

    /// <summary>Идентификатор платежа в T-Bank.</summary>
    [JsonConverter(typeof(TBankStringJsonConverter))]
    public string? PaymentId { get; init; }

    /// <summary>Статус платежа.</summary>
    [JsonConverter(typeof(TBankPaymentStatusJsonConverter))]
    public TBankPaymentStatus Status { get; init; }

    /// <summary>Сумма операции.</summary>
    [JsonConverter(typeof(TBankAmountJsonConverter))]
    public TBankAmount? Amount { get; init; }

    /// <summary>Код причины отказа в возврате по СБП.</summary>
    public string? QrCancelCode { get; init; }

    /// <summary>Описание причины отказа в возврате по СБП.</summary>
    public string? QrCancelMessage { get; init; }
}

/// <summary>
/// Ответ метода GetQrBankList.
/// </summary>
public sealed record TBankQrBankListResponse : TBankResponse
{
    /// <summary>Список банков-участников СБП от НСПК.</summary>
    public IReadOnlyList<TBankQrBank> BankList { get; init; } = [];
}

/// <summary>
/// Банк-участник СБП в ответе GetQrBankList.
/// </summary>
public sealed record TBankQrBank
{
    /// <summary>Внутренний идентификатор банка.</summary>
    public string? BankId { get; init; }

    /// <summary>Идентификатор банка в системе НСПК.</summary>
    public string? NspkBankId { get; init; }

    /// <summary>Наименование банка.</summary>
    public string? BankName { get; init; }

    /// <summary>Ссылка на логотип банка.</summary>
    public string? BankLogo { get; init; }

    /// <summary>Порядок для сортировки.</summary>
    [JsonConverter(typeof(TBankInt32JsonConverter))]
    public int BankOrder { get; init; }
}

/// <summary>
/// Ответ метода SbpPayTest.
/// </summary>
public sealed record TBankSbpPayTestResponse : TBankResponse
{
}

/// <summary>
/// Ответ метода AddAccountQr.
/// </summary>
public sealed record TBankAddAccountQrResponse : TBankResponse
{
    /// <summary>Ключ терминала.</summary>
    public string? TerminalKey { get; init; }

    /// <summary>Подробное описание деталей заказа.</summary>
    public string? Description { get; init; }

    /// <summary>Тип возвращаемых данных QR.</summary>
    [JsonConverter(typeof(TBankQrDataTypeJsonConverter))]
    public TBankQrDataType? DataType { get; init; }

    /// <summary>QR payload, deeplink или SVG-изображение в зависимости от DataType.</summary>
    public string? Data { get; init; }

    /// <summary>Идентификатор запроса на привязку счета.</summary>
    public string? RequestKey { get; init; }
}

/// <summary>
/// Ответ метода QrMembersList.
/// </summary>
public sealed record TBankQrMembersListResponse : TBankResponse
{
    /// <summary>Список участников. Возвращается, только если возврат возможен.</summary>
    public IReadOnlyList<TBankQrMember> Members { get; init; } = [];

    /// <summary>Номер заказа на стороне площадки.</summary>
    public string? OrderId { get; init; }
}

/// <summary>
/// Банк-участник QR в ответе QrMembersList.
/// </summary>
public sealed record TBankQrMember
{
    /// <summary>Идентификатор банка-участника.</summary>
    public string? MemberId { get; init; }

    /// <summary>Наименование банка-участника.</summary>
    public string? MemberName { get; init; }

    /// <summary>Признак того, что на счет участника возможен возврат.</summary>
    public bool IsPayee { get; init; }
}

/// <summary>
/// Ответ метода GetAccountQrList.
/// </summary>
public sealed record TBankAccountQrListResponse : TBankResponse
{
    /// <summary>Ключ терминала.</summary>
    public string? TerminalKey { get; init; }

    /// <summary>Список привязанных к магазину счетов покупателя.</summary>
    public IReadOnlyList<TBankAccountQr> AccountTokens { get; init; } = [];
}

/// <summary>
/// Привязанный счет покупателя в ответе GetAccountQrList.
/// </summary>
public sealed record TBankAccountQr
{
    /// <summary>Идентификатор запроса на привязку счета.</summary>
    public string? RequestKey { get; init; }

    /// <summary>Статус привязки счета.</summary>
    [JsonConverter(typeof(TBankAccountQrStatusJsonConverter))]
    public TBankAccountQrStatus Status { get; init; }

    /// <summary>Идентификатор привязки счета, назначаемый банком плательщика.</summary>
    [JsonConverter(typeof(TBankStringJsonConverter))]
    public string? AccountToken { get; init; }

    /// <summary>Идентификатор банка покупателя. Заполнен для статусов ACTIVE и INACTIVE.</summary>
    public string? BankMemberId { get; init; }

    /// <summary>Наименование банка-эмитента. Заполнено, если передан BankMemberId.</summary>
    public string? BankMemberName { get; init; }
}

/// <summary>
/// Ответ метода GetAddAccountQrState.
/// </summary>
public sealed record TBankAddAccountQrStateResponse : TBankResponse
{
    /// <summary>Ключ терминала.</summary>
    public string? TerminalKey { get; init; }

    /// <summary>Идентификатор запроса на привязку счета.</summary>
    [JsonConverter(typeof(TBankStringJsonConverter))]
    public string? RequestKey { get; init; }

    /// <summary>Идентификатор банка покупателя. Заполнен для статусов ACTIVE и INACTIVE.</summary>
    public string? BankMemberId { get; init; }

    /// <summary>Наименование банка-эмитента. Заполнено, если передан BankMemberId.</summary>
    public string? BankMemberName { get; init; }

    /// <summary>Идентификатор привязки счета, назначаемый банком плательщика.</summary>
    [JsonConverter(typeof(TBankStringJsonConverter))]
    public string? AccountToken { get; init; }

    /// <summary>Статус привязки счета.</summary>
    [JsonConverter(typeof(TBankAccountQrStatusJsonConverter))]
    public TBankAccountQrStatus Status { get; init; }
}

/// <summary>
/// Ответ метода ChargeQr.
/// </summary>
public sealed record TBankChargeQrResponse : TBankResponse
{
    /// <summary>Ключ терминала.</summary>
    public string? TerminalKey { get; init; }

    /// <summary>Номер заказа на стороне площадки.</summary>
    public string? OrderId { get; init; }

    /// <summary>Идентификатор платежа в T-Bank.</summary>
    [JsonConverter(typeof(TBankStringJsonConverter))]
    public string? PaymentId { get; init; }

    /// <summary>Статус платежа.</summary>
    [JsonConverter(typeof(TBankPaymentStatusJsonConverter))]
    public TBankPaymentStatus Status { get; init; }

    /// <summary>Сумма списания.</summary>
    [JsonConverter(typeof(TBankAmountJsonConverter))]
    public TBankAmount? Amount { get; init; }

    /// <summary>Код валюты ISO 4217.</summary>
    [JsonConverter(typeof(TBankInt32JsonConverter))]
    public int? Currency { get; init; }
}
