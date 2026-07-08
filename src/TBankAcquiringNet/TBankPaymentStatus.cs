namespace TBankAcquiringNet;

/// <summary>
/// Статус платежа в T-Bank acquiring. Имена совпадают с проводными значениями API.
/// </summary>
public enum TBankPaymentStatus
{
    /// <summary>Значение по умолчанию (статус не задан). API это значение не возвращает.</summary>
    UNKNOWN,

    /// <summary>Платеж зарегистрирован, обработка еще не начата.</summary>
    NEW,

    /// <summary>Платеж отменен площадкой.</summary>
    CANCELED,

    /// <summary>Проверяются платежные данные покупателя.</summary>
    PREAUTHORIZING,

    /// <summary>Покупатель был перенаправлен на платежную форму.</summary>
    FORM_SHOWED,

    /// <summary>Платеж проходит авторизацию.</summary>
    AUTHORIZING,

    /// <summary>Начата проверка 3-D Secure (проводное значение 3DS_CHECKING).</summary>
    THREE_DS_CHECKING,

    /// <summary>Проверка 3-D Secure завершена (проводное значение 3DS_CHECKED).</summary>
    THREE_DS_CHECKED,

    /// <summary>Деньги захолдированы на карте, ожидается подтверждение.</summary>
    AUTHORIZED,

    /// <summary>Платеж обрабатывается, финальный статус будет присвоен позже.</summary>
    PAY_CHECKING,

    /// <summary>Начато подтверждение списания.</summary>
    CONFIRMING,

    /// <summary>Подтверждение платежа обрабатывается, финальный статус будет присвоен позже.</summary>
    CONFIRM_CHECKING,

    /// <summary>Платеж подтвержден, деньги списаны.</summary>
    CONFIRMED,

    /// <summary>Начата отмена холдирования средств.</summary>
    REVERSING,

    /// <summary>Выполнена частичная отмена авторизованного платежа.</summary>
    PARTIAL_REVERSED,

    /// <summary>Выполнена полная отмена авторизованного платежа.</summary>
    REVERSED,

    /// <summary>Начат возврат денежных средств.</summary>
    REFUNDING,

    /// <summary>Обрабатывается возврат денежных средств по QR.</summary>
    ASYNC_REFUNDING,

    /// <summary>Выполнен частичный возврат подтвержденного платежа.</summary>
    PARTIAL_REFUNDED,

    /// <summary>Выполнен полный возврат подтвержденного платежа.</summary>
    REFUNDED,

    /// <summary>Выполняется проверка платежа в процессе его отмены.</summary>
    CANCEL_CHECKING,

    /// <summary>Срок активности платежной сессии истек.</summary>
    DEADLINE_EXPIRED,

    /// <summary>Платеж отклонен банком.</summary>
    REJECTED,

    /// <summary>Ошибка платежа или не пройдена проверка 3-D Secure.</summary>
    AUTH_FAIL,

    /// <summary>Выполняется проверка платежа.</summary>
    CHECKING,

    /// <summary>Проверка платежа завершена.</summary>
    CHECKED,

    /// <summary>Начато завершение расчётов по платежу.</summary>
    COMPLETING,

    /// <summary>Расчёты по платежу завершены.</summary>
    COMPLETED,

    /// <summary>Платёж обрабатывается.</summary>
    PROCESSING
}
