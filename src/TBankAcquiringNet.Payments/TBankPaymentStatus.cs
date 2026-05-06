namespace TBankAcquiringNet.Payments;

/// <summary>
/// Статус платежа в T-Bank acquiring.
/// </summary>
public enum TBankPaymentStatus
{
    /// <summary>Неизвестный или пока не смоделированный SDK статус.</summary>
    Unknown,

    /// <summary>Платеж зарегистрирован, обработка еще не начата.</summary>
    New,

    /// <summary>Платеж отменен площадкой.</summary>
    Canceled,

    /// <summary>Проверяются платежные данные покупателя.</summary>
    Preauthorizing,

    /// <summary>Покупатель был перенаправлен на платежную форму.</summary>
    FormShowed,

    /// <summary>Платеж проходит авторизацию.</summary>
    Authorizing,

    /// <summary>Начата проверка 3-D Secure.</summary>
    ThreeDsChecking,

    /// <summary>Проверка 3-D Secure завершена.</summary>
    ThreeDsChecked,

    /// <summary>Деньги захолдированы на карте, ожидается подтверждение.</summary>
    Authorized,

    /// <summary>Платеж обрабатывается, финальный статус будет присвоен позже.</summary>
    PayChecking,

    /// <summary>Начато подтверждение списания.</summary>
    Confirming,

    /// <summary>Платеж подтвержден, деньги списаны.</summary>
    Confirmed,

    /// <summary>Начата отмена холдирования средств.</summary>
    Reversing,

    /// <summary>Выполнена частичная отмена авторизованного платежа.</summary>
    PartialReversed,

    /// <summary>Выполнена полная отмена авторизованного платежа.</summary>
    Reversed,

    /// <summary>Начат возврат денежных средств.</summary>
    Refunding,

    /// <summary>Выполнен частичный возврат подтвержденного платежа.</summary>
    PartialRefunded,

    /// <summary>Выполнен полный возврат подтвержденного платежа.</summary>
    Refunded,

    /// <summary>Срок активности платежной сессии истек.</summary>
    DeadlineExpired,

    /// <summary>Платеж отклонен банком.</summary>
    Rejected,

    /// <summary>Ошибка платежа или не пройдена проверка 3-D Secure.</summary>
    AuthFail,

    /// <summary>Подтверждение платежа обрабатывается, финальный статус будет присвоен позже.</summary>
    ConfirmChecking,

    /// <summary>Обрабатывается возврат денежных средств по QR.</summary>
    AsyncRefunding
}
