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

        return value switch
        {
            "NEW" => TBankPaymentStatus.NEW,
            "CANCELED" => TBankPaymentStatus.CANCELED,
            "PREAUTHORIZING" => TBankPaymentStatus.PREAUTHORIZING,
            "FORM_SHOWED" => TBankPaymentStatus.FORM_SHOWED,
            "AUTHORIZING" => TBankPaymentStatus.AUTHORIZING,
            "3DS_CHECKING" => TBankPaymentStatus.THREE_DS_CHECKING,
            "3DS_CHECKED" => TBankPaymentStatus.THREE_DS_CHECKED,
            "AUTHORIZED" => TBankPaymentStatus.AUTHORIZED,
            "PAY_CHECKING" => TBankPaymentStatus.PAY_CHECKING,
            "CONFIRMING" => TBankPaymentStatus.CONFIRMING,
            "CONFIRM_CHECKING" => TBankPaymentStatus.CONFIRM_CHECKING,
            "CONFIRMED" => TBankPaymentStatus.CONFIRMED,
            "REVERSING" => TBankPaymentStatus.REVERSING,
            "PARTIAL_REVERSED" => TBankPaymentStatus.PARTIAL_REVERSED,
            "REVERSED" => TBankPaymentStatus.REVERSED,
            "REFUNDING" => TBankPaymentStatus.REFUNDING,
            "ASYNC_REFUNDING" => TBankPaymentStatus.ASYNC_REFUNDING,
            "CANCEL_CHECKING" => TBankPaymentStatus.CANCEL_CHECKING,
            "PARTIAL_REFUNDED" => TBankPaymentStatus.PARTIAL_REFUNDED,
            "REFUNDED" => TBankPaymentStatus.REFUNDED,
            "DEADLINE_EXPIRED" => TBankPaymentStatus.DEADLINE_EXPIRED,
            "REJECTED" => TBankPaymentStatus.REJECTED,
            "AUTH_FAIL" => TBankPaymentStatus.AUTH_FAIL,
            "CHECKING" => TBankPaymentStatus.CHECKING,
            "CHECKED" => TBankPaymentStatus.CHECKED,
            "COMPLETING" => TBankPaymentStatus.COMPLETING,
            "COMPLETED" => TBankPaymentStatus.COMPLETED,
            "PROCESSING" => TBankPaymentStatus.PROCESSING,
            _ => throw TBankWireParsing.UnknownEnumValue("payment status", value)
        };
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
