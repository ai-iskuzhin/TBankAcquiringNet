using System.Collections;
using System.Collections.Generic;
using System.Globalization;
using System.Reflection;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
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

        var values = GetWireValues(payload) ?? GetTokenValues(payload);
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

    /// <summary>
    /// Поля, снятые с провода при разборе, если payload их сохранил.
    /// </summary>
    /// <remarks>
    /// Подпись банк считает по присланному, а не по разобранному. Модель может не знать поля, не
    /// разобрать статус или сформатировать число иначе — тогда подпись по модели не совпадёт,
    /// хотя нотификация подлинная.
    /// </remarks>
    private static SortedDictionary<string, string>? GetWireValues<TPayload>(TPayload payload)
    {
        if (payload!.GetType().GetProperty("RawFields")?.GetValue(payload)
            is not IEnumerable<KeyValuePair<string, string>> wireFields)
        {
            return null;
        }

        var values = new SortedDictionary<string, string>(StringComparer.Ordinal);
        foreach (var field in wireFields)
        {
            if (!string.Equals(field.Key, "Token", StringComparison.Ordinal))
            {
                values[field.Key] = field.Value;
            }
        }

        return values.Count == 0 ? null : values;
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

            // Поле, которого нет на проводе, банк подписать не мог; отмеченное — не подписывает.
            if (property.GetCustomAttribute<JsonIgnoreAttribute>() is not null
                || property.GetCustomAttribute<TBankUnsignedFieldAttribute>() is not null)
            {
                continue;
            }

            var key = GetWireName(property);
            if (string.Equals(key, "Token", StringComparison.Ordinal))
            {
                continue;
            }

            var value = property.GetValue(payload);
            if (value is null)
            {
                continue;
            }

            // Поля, которых нет в модели, банк подписывает наравне с известными, поэтому словарь
            // расширения раскрывается в отдельные значения, а не пропускается как перечисление.
            if (property.GetCustomAttribute<JsonExtensionDataAttribute>() is not null)
            {
                AddExtensionValues(values, value);
                continue;
            }

            if (!TryFormatTokenValue(value, out var formattedValue))
            {
                continue;
            }

            values[key] = formattedValue;
        }

        return values;
    }

    private static void AddExtensionValues(SortedDictionary<string, string> values, object extensionData)
    {
        if (extensionData is not IEnumerable entries)
        {
            return;
        }

        foreach (var entry in entries)
        {
            if (entry is null)
            {
                continue;
            }

            var entryType = entry.GetType();
            if (entryType.GetProperty("Key")?.GetValue(entry) is not string key
                || key.Length == 0
                || string.Equals(key, "Token", StringComparison.Ordinal))
            {
                continue;
            }

            var entryValue = entryType.GetProperty("Value")?.GetValue(entry);
            if (entryValue is null)
            {
                continue;
            }

            if (entryValue is JsonElement element)
            {
                if (TryFormatJsonElement(element, out var formattedElement))
                {
                    values[key] = formattedElement;
                }

                continue;
            }

            if (TryFormatTokenValue(entryValue, out var formattedValue))
            {
                values[key] = formattedValue;
            }
        }
    }

    /// <summary>
    /// Значение так, как его подписал банк. Объекты и массивы в подпись не входят.
    /// </summary>
    private static bool TryFormatJsonElement(JsonElement element, out string formattedValue)
    {
        switch (element.ValueKind)
        {
            case JsonValueKind.String:
                formattedValue = element.GetString() ?? string.Empty;
                return true;
            case JsonValueKind.Number:
                formattedValue = element.GetRawText();
                return true;
            case JsonValueKind.True:
                formattedValue = "true";
                return true;
            case JsonValueKind.False:
                formattedValue = "false";
                return true;
            default:
                formattedValue = string.Empty;
                return false;
        }
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

        // Вложенный объект или массив банк в подпись не берёт; скаляр, приехавший как JsonElement,
        // подписывается как обычное значение.
        if (value is JsonElement jsonElement)
        {
            return TryFormatJsonElement(jsonElement, out formattedValue);
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

        // Тем же форматом, которым дату пишет TBankDateTimeOffsetJsonConverter. Формат "O" давал
        // дробные доли секунды: подпись считалась по одной записи, а в теле уезжала другая, и банк
        // отвечал 204 «Неверный токен».
        if (value is DateTimeOffset dateTimeOffset)
        {
            formattedValue = TBankWireDates.FormatValue(dateTimeOffset);
            return true;
        }

        if (value is DateTime dateTime)
        {
            formattedValue = TBankWireDates.FormatValue(dateTime);
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
