using System.Collections;
using System.Globalization;
using System.Reflection;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json.Serialization;

namespace TBankAcquiringNet;

/// <summary>
/// Генерация и проверка SHA-256 Token для запросов и нотификаций T-Bank.
/// </summary>
/// <remarks>
/// Алгоритм T-Bank: взять верхнеуровневые скалярные поля, исключить Token, добавить Password,
/// отсортировать ключи по алфавиту, склеить значения и посчитать SHA-256 в нижнем hex-регистре.
/// </remarks>
/// <example>
/// <code>
/// var token = TBankToken.Create(new TBankPaymentStateRequest
/// {
///     TerminalKey = "TestB",
///     PaymentId = "20150"
/// }, password);
/// </code>
/// </example>
public static class TBankToken
{
    /// <summary>
    /// Создает Token для payload по правилам T-Bank.
    /// </summary>
    /// <typeparam name="TPayload">Тип запроса или нотификации.</typeparam>
    /// <param name="payload">Объект, из публичных свойств которого формируется подпись.</param>
    /// <param name="password">Пароль терминала T-Bank.</param>
    /// <returns>SHA-256 подпись в нижнем hex-регистре.</returns>
    public static string Create<TPayload>(TPayload payload, string password)
    {
        ArgumentNullException.ThrowIfNull(payload);

        if (string.IsNullOrEmpty(password))
        {
            throw new ArgumentException("Password must not be empty.", nameof(password));
        }

        var values = GetTokenValues(payload);
        values["Password"] = password;

        var tokenInput = string.Concat(values.OrderBy(static item => item.Key, StringComparer.Ordinal).Select(static item => item.Value));

        return ComputeSha256Hex(tokenInput);
    }

    /// <summary>
    /// Проверяет ожидаемый Token для payload.
    /// </summary>
    /// <typeparam name="TPayload">Тип запроса или нотификации.</typeparam>
    /// <param name="payload">Объект, для которого рассчитывается подпись.</param>
    /// <param name="password">Пароль терминала T-Bank.</param>
    /// <param name="expectedToken">Token, полученный от T-Bank или из тестового примера.</param>
    /// <returns>true, если рассчитанный Token совпадает с expectedToken.</returns>
    public static bool Verify<TPayload>(TPayload payload, string password, string expectedToken)
    {
        if (string.IsNullOrWhiteSpace(expectedToken))
        {
            return false;
        }

        var actualToken = Create(payload, password);
        return FixedTimeEquals(
            Encoding.ASCII.GetBytes(actualToken),
            Encoding.ASCII.GetBytes(expectedToken.Trim().ToLowerInvariant()));
    }

    private static string ComputeSha256Hex(string input)
    {
        var bytes = Encoding.UTF8.GetBytes(input);

#if NETSTANDARD2_0
        using var sha = SHA256.Create();
        var hash = sha.ComputeHash(bytes);

        var builder = new StringBuilder(hash.Length * 2);
        foreach (var b in hash)
        {
            builder.Append(b.ToString("x2", CultureInfo.InvariantCulture));
        }

        return builder.ToString();
#else
        var hash = SHA256.HashData(bytes);
        return Convert.ToHexString(hash).ToLowerInvariant();
#endif
    }

    private static bool FixedTimeEquals(byte[] left, byte[] right)
    {
#if NETSTANDARD2_0
        if (left.Length != right.Length)
        {
            return false;
        }

        var difference = 0;
        for (var i = 0; i < left.Length; i++)
        {
            difference |= left[i] ^ right[i];
        }

        return difference == 0;
#else
        return CryptographicOperations.FixedTimeEquals(left, right);
#endif
    }

    private static SortedDictionary<string, string> GetTokenValues<TPayload>(TPayload payload)
    {
        var values = new SortedDictionary<string, string>(StringComparer.Ordinal);
        var properties = payload!.GetType().GetProperties(BindingFlags.Instance | BindingFlags.Public);

        foreach (var property in properties)
        {
            if (property.GetMethod is null || property.GetMethod.GetParameters().Length != 0)
            {
                continue;
            }

            var key = GetWireName(property);
            if (string.Equals(key, "Token", StringComparison.Ordinal))
            {
                continue;
            }

            var value = property.GetValue(payload);
            if (value is null || !TryFormatTokenValue(value, out var formattedValue))
            {
                continue;
            }

            values[key] = formattedValue;
        }

        return values;
    }

    private static string GetWireName(PropertyInfo property)
    {
        var jsonPropertyName = property.GetCustomAttribute<JsonPropertyNameAttribute>();
        return jsonPropertyName?.Name ?? property.Name;
    }

    private static bool TryFormatTokenValue(object value, out string formattedValue)
    {
        formattedValue = string.Empty;

        if (value is string text)
        {
            formattedValue = text;
            return true;
        }

        if (value is TBankAmount amount)
        {
            formattedValue = amount.MinorUnits.ToString(CultureInfo.InvariantCulture);
            return true;
        }

        if (value is Uri uri)
        {
            formattedValue = uri.ToString();
            return true;
        }

        var valueType = value.GetType();
        var underlyingType = Nullable.GetUnderlyingType(valueType) ?? valueType;

        if (underlyingType.IsEnum)
        {
            formattedValue = value switch
            {
                TBankPaymentStatus paymentStatus => TBankWireNames.FormatPaymentStatus(paymentStatus),
                TBankQrDataType qrDataType => TBankWireNames.FormatQrDataType(qrDataType),
                TBankPayType payType => TBankWireNames.FormatPayType(payType),
                TBankLanguage language => TBankWireNames.FormatLanguage(language),
                TBankRecurrent recurrent => TBankWireNames.FormatRecurrent(recurrent),
                TBankAccountQrStatus accountQrStatus => TBankWireNames.FormatAccountQrStatus(accountQrStatus),
                _ => value.ToString() ?? string.Empty
            };
            return true;
        }

        if (value is bool boolValue)
        {
            formattedValue = boolValue ? "true" : "false";
            return true;
        }

        if (value is IFormattable formattable && IsNumericType(underlyingType))
        {
            formattedValue = formattable.ToString(null, CultureInfo.InvariantCulture);
            return true;
        }

        if (value is DateTimeOffset dateTimeOffset)
        {
            formattedValue = dateTimeOffset.ToString("O", CultureInfo.InvariantCulture);
            return true;
        }

        if (value is DateTime dateTime)
        {
            formattedValue = dateTime.ToString("O", CultureInfo.InvariantCulture);
            return true;
        }

        if (value is IEnumerable and not string)
        {
            return false;
        }

        return false;
    }

    private static bool IsNumericType(Type type)
    {
        return type == typeof(byte)
            || type == typeof(sbyte)
            || type == typeof(short)
            || type == typeof(ushort)
            || type == typeof(int)
            || type == typeof(uint)
            || type == typeof(long)
            || type == typeof(ulong)
            || type == typeof(float)
            || type == typeof(double)
            || type == typeof(decimal);
    }
}
