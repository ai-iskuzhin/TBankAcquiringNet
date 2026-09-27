using System.Globalization;

namespace TBankAcquiringNet;

/// <summary>
/// Формат даты и времени на проводе T-Bank.
/// </summary>
/// <remarks>
/// Банк принимает дату с точностью до секунды и отвергает дробные доли: <c>Init</c> с
/// <c>2026-09-26T12:00:00.1234567+05:00</c> возвращает <c>9999</c> «Повторите попытку позже» без
/// подробностей. Формат один и тот же для тела запроса и для подписи: разойдись они — и банк ответит
/// <c>204</c> «Неверный токен», потому что подпись посчитана не по тому, что уехало.
/// </remarks>
internal static class TBankWireDates
{
    private const string Format = "yyyy-MM-ddTHH:mm:sszzz";

    public static string FormatValue(DateTimeOffset value) =>
        value.ToString(Format, CultureInfo.InvariantCulture);

    public static string FormatValue(DateTime value) =>
        FormatValue(new DateTimeOffset(value));

    public static bool TryParse(string? text, out DateTimeOffset value) =>
        DateTimeOffset.TryParse(
            text,
            CultureInfo.InvariantCulture,
            DateTimeStyles.RoundtripKind,
            out value);
}
