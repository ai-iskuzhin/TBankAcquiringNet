using System.Globalization;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace TBankAcquiringNet.Payments;

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
            "NEW" => TBankPaymentStatus.New,
            "CANCELED" => TBankPaymentStatus.Canceled,
            "PREAUTHORIZING" => TBankPaymentStatus.Preauthorizing,
            "FORM_SHOWED" => TBankPaymentStatus.FormShowed,
            "AUTHORIZING" => TBankPaymentStatus.Authorizing,
            "3DS_CHECKING" => TBankPaymentStatus.ThreeDsChecking,
            "3DS_CHECKED" => TBankPaymentStatus.ThreeDsChecked,
            "AUTHORIZED" => TBankPaymentStatus.Authorized,
            "PAY_CHECKING" => TBankPaymentStatus.PayChecking,
            "CONFIRMING" => TBankPaymentStatus.Confirming,
            "CONFIRM_CHECKING" => TBankPaymentStatus.ConfirmChecking,
            "CONFIRMED" => TBankPaymentStatus.Confirmed,
            "REVERSING" => TBankPaymentStatus.Reversing,
            "PARTIAL_REVERSED" => TBankPaymentStatus.PartialReversed,
            "REVERSED" => TBankPaymentStatus.Reversed,
            "REFUNDING" => TBankPaymentStatus.Refunding,
            "ASYNC_REFUNDING" => TBankPaymentStatus.AsyncRefunding,
            "PARTIAL_REFUNDED" => TBankPaymentStatus.PartialRefunded,
            "REFUNDED" => TBankPaymentStatus.Refunded,
            "DEADLINE_EXPIRED" => TBankPaymentStatus.DeadlineExpired,
            "REJECTED" => TBankPaymentStatus.Rejected,
            "AUTH_FAIL" => TBankPaymentStatus.AuthFail,
            _ => TBankPaymentStatus.Unknown
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
