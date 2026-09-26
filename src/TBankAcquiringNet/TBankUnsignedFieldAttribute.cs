namespace TBankAcquiringNet;

/// <summary>
/// Поле едет на проводе, но в подпись не входит.
/// </summary>
/// <remarks>
/// Банк подписывает только скалярные поля верхнего уровня. Вложенные объекты и массивы исключаются
/// сами — форматировать их нечем; а вот поле, которое банк присылает объектом, а SDK хранит строкой
/// (<see cref="TBankPaymentNotification.DATA"/>), выглядит для подписи обычным скаляром и попало бы
/// в неё. Отсюда явная отметка.
/// </remarks>
[AttributeUsage(AttributeTargets.Property)]
internal sealed class TBankUnsignedFieldAttribute : Attribute
{
}
