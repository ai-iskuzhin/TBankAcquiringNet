using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using TBankAcquiringNet;

namespace TBankAcquiringNet.Tests;

/// <summary>
/// Подпись должна совпадать с тем, что уехало в теле. Для даты это не совпадало ни при каком
/// значении: целые секунды давали телу <c>…T12:00:00+05:00</c> против подписи с
/// <c>.0000000</c> (банк отвечал 204 «Неверный токен»), а дробные доли уезжали в теле и банк
/// отвечал 9999 без подробностей.
/// </summary>
public sealed class TBankWireDateTests
{
    private static readonly JsonSerializerOptions BodyJson = new()
    {
        DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull
    };

    [Theory]
    [InlineData(0)]          // целые секунды
    [InlineData(1234567)]    // с дробной долей
    [InlineData(9999999)]
    public void InitRequest_TokenMatchesTheBodyThatIsActuallySent(long extraTicks)
    {
        const string password = "Dfsfh56dgKl";
        var request = new TBankInitPaymentRequest
        {
            TerminalKey = "TinkoffBankTest",
            Amount = TBankAmount.FromMinorUnits(5000),
            OrderId = "SGH-1",
            RedirectDueDate = new DateTimeOffset(2026, 9, 26, 12, 0, 0, TimeSpan.FromHours(5)).AddTicks(extraTicks)
        };

        var token = TBankToken.Create(request, password);

        // Пересчитываем подпись по телу, а не по модели: именно тело видит банк.
        using var document = JsonDocument.Parse(JsonSerializer.Serialize(request, BodyJson));
        var signed = new SortedDictionary<string, string>(StringComparer.Ordinal) { ["Password"] = password };
        foreach (var property in document.RootElement.EnumerateObject())
        {
            if (property.Value.ValueKind is JsonValueKind.Object or JsonValueKind.Array
                || string.Equals(property.Name, "Token", StringComparison.Ordinal))
            {
                continue;
            }

            signed[property.Name] = property.Value.ValueKind switch
            {
                JsonValueKind.String => property.Value.GetString() ?? string.Empty,
                JsonValueKind.True => "true",
                JsonValueKind.False => "false",
                _ => property.Value.GetRawText()
            };
        }

        var fromBody = Convert.ToHexString(
                SHA256.HashData(Encoding.UTF8.GetBytes(string.Concat(signed.Values))))
            .ToLowerInvariant();

        Assert.Equal(fromBody, token);
    }

    [Fact]
    public void InitRequest_SendsTheDateWithoutFractionalSeconds()
    {
        var request = new TBankInitPaymentRequest
        {
            TerminalKey = "T",
            Amount = TBankAmount.FromMinorUnits(5000),
            OrderId = "SGH-1",
            RedirectDueDate = new DateTimeOffset(2026, 9, 26, 12, 0, 0, TimeSpan.FromHours(5)).AddTicks(1234567)
        };

        using var document = JsonDocument.Parse(JsonSerializer.Serialize(request, BodyJson));

        Assert.Equal(
            "2026-09-26T12:00:00+05:00",
            document.RootElement.GetProperty("RedirectDueDate").GetString());
    }
}
