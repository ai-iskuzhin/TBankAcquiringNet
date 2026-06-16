namespace TBankAcquiringNet;

internal static class TBankWireParsing
{
    /// <summary>
    /// Создает исключение для проводного значения перечисления, которое SDK пока не сопоставляет.
    /// Сигнализирует о пробеле в библиотеке и просит завести issue.
    /// </summary>
    public static NotImplementedException UnknownEnumValue(string fieldDescription, string? value)
    {
        return new NotImplementedException(
            $"T-Bank returned an unknown {fieldDescription} value '{value}' that TBankAcquiringNet does not map yet. " +
            "This is a gap in the library, not a usage error — please open an issue at " +
            "https://github.com/ai-iskuzhin/TBankAcquiringNet/issues/new.");
    }
}
