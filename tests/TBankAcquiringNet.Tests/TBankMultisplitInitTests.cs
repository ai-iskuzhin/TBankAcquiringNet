using System.Text.Json;
using System.Text.Json.Serialization;
using TBankAcquiringNet;

namespace TBankAcquiringNet.Tests;

/// <summary>
/// Поля multisplit-терминала в <c>Init</c>: имена на проводе и участие в подписи. Имя, разошедшееся
/// со спецификацией, банк молча проигнорирует — платёж уйдёт не в ту сделку или без получателя.
/// </summary>
public sealed class TBankMultisplitInitTests
{
    private static readonly JsonSerializerOptions BodyJson = new()
    {
        DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull
    };

    private static TBankInitPaymentRequest Request() => new()
    {
        TerminalKey = "TinkoffBankTest",
        Amount = TBankAmount.FromMinorUnits(15000),
        OrderId = "sp123",
        PaymentRecipientId = "3145181",
        DealId = "91954170",
        CreateDealWithType = "NN",
        LevelOfConfidence = "high",
        Descriptor = "SGH-0000000002",
        StartSpAccumulation = "NN",
        SpAccumulationId = "91949833",
        BasicFieldKey = "+79279383562",
        Confidant = "moderate"
    };

    [Theory]
    [InlineData("PaymentRecipientId", "3145181")]
    [InlineData("DealId", "91954170")]
    [InlineData("CreateDealWithType", "NN")]
    [InlineData("LevelOfConfidence", "high")]
    [InlineData("Descriptor", "SGH-0000000002")]
    [InlineData("StartSpAccumulation", "NN")]
    [InlineData("SpAccumulationId", "91949833")]
    [InlineData("BasicFieldKey", "+79279383562")]
    [InlineData("Confidant", "moderate")]
    public void Init_SendsTheMultisplitFieldUnderItsSpecName(string wireName, string expected)
    {
        using var document = JsonDocument.Parse(JsonSerializer.Serialize(Request(), BodyJson));

        Assert.True(document.RootElement.TryGetProperty(wireName, out var value), $"{wireName} is absent from the body");
        Assert.Equal(expected, value.GetString());
    }

    [Fact]
    public void Init_SignsTheMultisplitFields()
    {
        // Подпись считается по всем скалярным полям верхнего уровня, значит новые поля обязаны в неё
        // попадать: иначе банк ответит «Неверный токен» ровно на multisplit-платежи.
        var withFields = TBankToken.Create(Request(), "Dfsfh56dgKl");
        var withoutFields = TBankToken.Create(
            Request() with
            {
                Descriptor = null,
                StartSpAccumulation = null,
                SpAccumulationId = null,
                BasicFieldKey = null,
                Confidant = null
            },
            "Dfsfh56dgKl");

        Assert.NotEqual(withoutFields, withFields);
    }

    [Fact]
    public void Init_OmitsTheMultisplitFieldsOnAnOrdinaryTerminal()
    {
        var plain = new TBankInitPaymentRequest
        {
            TerminalKey = "TinkoffBankTest",
            Amount = TBankAmount.FromMinorUnits(15000),
            OrderId = "sp123"
        };

        using var document = JsonDocument.Parse(JsonSerializer.Serialize(plain, BodyJson));

        foreach (var name in new[]
        {
            "PaymentRecipientId", "DealId", "CreateDealWithType", "LevelOfConfidence",
            "Descriptor", "StartSpAccumulation", "SpAccumulationId", "BasicFieldKey", "Confidant"
        })
        {
            Assert.False(document.RootElement.TryGetProperty(name, out _), $"{name} must not be sent when unset");
        }
    }
}
