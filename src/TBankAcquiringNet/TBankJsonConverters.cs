using System.Globalization;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace TBankAcquiringNet;

internal sealed class TBankAmountJsonConverter : JsonConverter<TBankAmount>
{
    public override TBankAmount Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
    {
        return reader.TokenType switch
        {
            JsonTokenType.Number => new TBankAmount(reader.GetInt64()),
            JsonTokenType.String when long.TryParse(reader.GetString(), NumberStyles.Integer, CultureInfo.InvariantCulture, out var amount) => new TBankAmount(amount),
            _ => throw new JsonException("Expected T-Bank amount as number or numeric string.")
        };
    }

    public override void Write(Utf8JsonWriter writer, TBankAmount value, JsonSerializerOptions options)
    {
        writer.WriteNumberValue(value.MinorUnits);
    }
}

internal sealed class TBankStringJsonConverter : JsonConverter<string>
{
    public override string? Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
    {
        return reader.TokenType switch
        {
            JsonTokenType.String => reader.GetString(),
            JsonTokenType.Number when reader.TryGetInt64(out var integer) => integer.ToString(CultureInfo.InvariantCulture),
            JsonTokenType.Number => reader.GetDouble().ToString("R", CultureInfo.InvariantCulture),
            JsonTokenType.True => "true",
            JsonTokenType.False => "false",
            JsonTokenType.Null => null,
            _ => throw new JsonException("Expected string-compatible JSON value.")
        };
    }

    public override void Write(Utf8JsonWriter writer, string value, JsonSerializerOptions options)
    {
        writer.WriteStringValue(value);
    }
}

internal sealed class TBankInt32JsonConverter : JsonConverter<int>
{
    public override int Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
    {
        return reader.TokenType switch
        {
            JsonTokenType.Number => reader.GetInt32(),
            JsonTokenType.String when int.TryParse(reader.GetString(), NumberStyles.Integer, CultureInfo.InvariantCulture, out var value) => value,
            _ => throw new JsonException("Expected integer as number or numeric string.")
        };
    }

    public override void Write(Utf8JsonWriter writer, int value, JsonSerializerOptions options)
    {
        writer.WriteNumberValue(value);
    }
}

internal sealed class TBankPaymentStatusJsonConverter : JsonConverter<TBankPaymentStatus>
{
    public override TBankPaymentStatus Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
    {
        var value = reader.GetString();

        return TBankWireNames.TryParsePaymentStatus(value, out var status)
            ? status
            : throw TBankWireParsing.UnknownEnumValue("payment status", value);
    
    }

    public override void Write(Utf8JsonWriter writer, TBankPaymentStatus value, JsonSerializerOptions options)
    {
        writer.WriteStringValue(TBankWireNames.FormatPaymentStatus(value));
    }
}

internal sealed class TBankQrDataTypeJsonConverter : JsonConverter<TBankQrDataType>
{
    public override TBankQrDataType Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
    {
        return reader.GetString() switch
        {
            "PAYLOAD" => TBankQrDataType.Payload,
            "IMAGE" => TBankQrDataType.Image,
            _ => throw new JsonException("Unknown T-Bank QR data type.")
        };
    }

    public override void Write(Utf8JsonWriter writer, TBankQrDataType value, JsonSerializerOptions options)
    {
        writer.WriteStringValue(TBankWireNames.FormatQrDataType(value));
    }
}

internal sealed class TBankPayTypeJsonConverter : JsonConverter<TBankPayType>
{
    public override TBankPayType Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
    {
        return reader.GetString() switch
        {
            "O" => TBankPayType.OneStage,
            "T" => TBankPayType.TwoStage,
            _ => throw new JsonException("Unknown T-Bank pay type.")
        };
    }

    public override void Write(Utf8JsonWriter writer, TBankPayType value, JsonSerializerOptions options)
    {
        writer.WriteStringValue(TBankWireNames.FormatPayType(value));
    }
}

internal sealed class TBankLanguageJsonConverter : JsonConverter<TBankLanguage>
{
    public override TBankLanguage Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
    {
        return reader.GetString() switch
        {
            "ru" => TBankLanguage.Ru,
            "en" => TBankLanguage.En,
            _ => throw new JsonException("Unknown T-Bank language.")
        };
    }

    public override void Write(Utf8JsonWriter writer, TBankLanguage value, JsonSerializerOptions options)
    {
        writer.WriteStringValue(TBankWireNames.FormatLanguage(value));
    }
}

internal sealed class TBankRecurrentJsonConverter : JsonConverter<TBankRecurrent>
{
    public override TBankRecurrent Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
    {
        return reader.GetString() switch
        {
            "Y" => TBankRecurrent.Yes,
            _ => throw new JsonException("Unknown T-Bank recurrent flag.")
        };
    }

    public override void Write(Utf8JsonWriter writer, TBankRecurrent value, JsonSerializerOptions options)
    {
        writer.WriteStringValue(TBankWireNames.FormatRecurrent(value));
    }
}

internal sealed class TBankCardStatusJsonConverter : JsonConverter<TBankCardStatus>
{
    public override TBankCardStatus Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
    {
        var value = reader.GetString();

        return value switch
        {
            "A" => TBankCardStatus.ACTIVE,
            "D" => TBankCardStatus.DELETED,
            _ => throw TBankWireParsing.UnknownEnumValue("card status", value)
        };
    }

    public override void Write(Utf8JsonWriter writer, TBankCardStatus value, JsonSerializerOptions options)
    {
        writer.WriteStringValue(TBankWireNames.FormatCardStatus(value));
    }
}

internal sealed class TBankAccountQrStatusJsonConverter : JsonConverter<TBankAccountQrStatus>
{
    public override TBankAccountQrStatus Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
    {
        var value = reader.GetString();

        return value switch
        {
            "NEW" => TBankAccountQrStatus.NEW,
            "PROCESSING" => TBankAccountQrStatus.PROCESSING,
            // "PROCCESING" is a known T-Bank misspelling observed on the wire.
            "PROCCESING" => TBankAccountQrStatus.PROCESSING,
            "ACTIVE" => TBankAccountQrStatus.ACTIVE,
            "INACTIVE" => TBankAccountQrStatus.INACTIVE,
            // "INACITVE" is a known T-Bank misspelling in the documentation.
            "INACITVE" => TBankAccountQrStatus.INACTIVE,
            _ => throw TBankWireParsing.UnknownEnumValue("account binding status", value)
        };
    }

    public override void Write(Utf8JsonWriter writer, TBankAccountQrStatus value, JsonSerializerOptions options)
    {
        writer.WriteStringValue(TBankWireNames.FormatAccountQrStatus(value));
    }
}

/// <summary>
/// Статус платежа в нотификации, терпимый к незнакомым значениям.
/// </summary>
/// <remarks>
/// В ответах на запросы незнакомый статус — это повод упасть: мы спросили и не поняли ответа.
/// В нотификации — нет: банк уже провёл операцию, и уронить разбор значит потерять событие о
/// деньгах из-за статуса, которого SDK ещё не знает. Такой статус становится
/// <see cref="TBankPaymentStatus.UNKNOWN"/>, а строка с провода остаётся в
/// <see cref="TBankPaymentNotification.StatusRaw"/>.
/// </remarks>
internal sealed class TBankNotificationPaymentStatusJsonConverter : JsonConverter<TBankPaymentStatus>
{
    public override TBankPaymentStatus Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
    {
        if (reader.TokenType != JsonTokenType.String)
        {
            return TBankPaymentStatus.UNKNOWN;
        }

        return TBankWireNames.TryParsePaymentStatus(reader.GetString(), out var status)
            ? status
            : TBankPaymentStatus.UNKNOWN;
    }

    public override void Write(Utf8JsonWriter writer, TBankPaymentStatus value, JsonSerializerOptions options)
    {
        writer.WriteStringValue(TBankWireNames.FormatPaymentStatus(value));
    }
}

/// <summary>
/// Поле, которое банк может прислать объектом, массивом или строкой: сохраняется как есть.
/// </summary>
internal sealed class TBankRawJsonJsonConverter : JsonConverter<string>
{
    public override string? Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
    {
        if (reader.TokenType == JsonTokenType.Null)
        {
            return null;
        }

        if (reader.TokenType == JsonTokenType.String)
        {
            return reader.GetString();
        }

        using var document = JsonDocument.ParseValue(ref reader);
        return document.RootElement.GetRawText();
    }

    public override void Write(Utf8JsonWriter writer, string value, JsonSerializerOptions options)
    {
        writer.WriteStringValue(value);
    }
}
