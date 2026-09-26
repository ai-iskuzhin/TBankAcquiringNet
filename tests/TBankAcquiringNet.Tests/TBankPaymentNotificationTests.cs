using System.Text.Json;
using TBankAcquiringNet;

namespace TBankAcquiringNet.Tests;

public sealed class TBankPaymentNotificationTests
{
    [Fact]
    public void ValidateToken_ReturnsValidForSignedNotification()
    {
        var notification = new TBankPaymentNotification
        {
            TerminalKey = "1510572937960",
            OrderId = "test2",
            Success = true,
            Status = TBankPaymentStatus.CONFIRMED,
            PaymentId = "2006896",
            ErrorCode = "0",
            Amount = TBankAmount.FromMinorUnits(102120),
            CardId = "867911",
            Pan = "430000**0777",
            ExpDate = "1122",
            Token = "51ac0bbcfa933b7807da8e14f527a74f4b6004b549b5efe899cd6ae5fb2639f1"
        };

        var result = TBankPaymentNotificationValidator.ValidateToken(notification, "Dfsfh56dgKl");

        Assert.Equal(TBankPaymentNotificationValidationResult.Valid, result);
    }

    [Fact]
    public void ValidateToken_ReturnsInvalidForChangedNotification()
    {
        var notification = new TBankPaymentNotification
        {
            TerminalKey = "1510572937960",
            OrderId = "test2",
            Success = true,
            Status = TBankPaymentStatus.CONFIRMED,
            PaymentId = "2006896",
            ErrorCode = "0",
            Amount = TBankAmount.FromMinorUnits(102121),
            CardId = "867911",
            Pan = "430000**0777",
            ExpDate = "1122",
            Token = "51ac0bbcfa933b7807da8e14f527a74f4b6004b549b5efe899cd6ae5fb2639f1"
        };

        var result = TBankPaymentNotificationValidator.ValidateToken(notification, "Dfsfh56dgKl");

        Assert.Equal(TBankPaymentNotificationValidationResult.InvalidToken, result);
    }

    [Fact]
    public void ValidateToken_ReturnsMissingToken()
    {
        var notification = new TBankPaymentNotification
        {
            TerminalKey = "1510572937960",
            OrderId = "test2",
            Success = true,
            Status = TBankPaymentStatus.CONFIRMED,
            PaymentId = "2006896",
            ErrorCode = "0",
            Token = ""
        };

        var result = TBankPaymentNotificationValidator.ValidateToken(notification, "Dfsfh56dgKl");

        Assert.Equal(TBankPaymentNotificationValidationResult.MissingToken, result);
    }

    [Fact]
    public void Notification_DeserializesNumericCardIdAndStatus()
    {
        var notification = JsonSerializer.Deserialize<TBankPaymentNotification>(
            """
            {
              "TerminalKey": "1510572937960",
              "OrderId": "test2",
              "Success": true,
              "Status": "CONFIRMED",
              "PaymentId": "2006896",
              "ErrorCode": "0",
              "Amount": 102120,
              "CardId": 867911,
              "Pan": "430000**0777",
              "ExpDate": "1122",
              "Token": "51ac0bbcfa933b7807da8e14f527a74f4b6004b549b5efe899cd6ae5fb2639f1"
            }
            """);

        Assert.NotNull(notification);
        Assert.Equal("867911", notification.CardId);
        Assert.Equal(TBankPaymentStatus.CONFIRMED, notification.Status);
        Assert.Equal(TBankAmount.FromMinorUnits(102120), notification.Amount);
    }

    [Fact]
    public void SuccessResponseBody_IsOk()
    {
        Assert.Equal("OK", TBankPaymentNotificationValidator.SuccessResponseBody);
    }

    // Подпись банк считает по всем присланным полям верхнего уровня. Поле, которого нет в модели,
    // раньше выпадало из подписи, и нотификация с ним отвергалась целиком.
    [Fact]
    public void ValidateToken_SignsRootFieldsTheModelDoesNotDeclare()
    {
        const string password = "Dfsfh56dgKl";
        const string json = """
            {"TerminalKey":"1510572937960","OrderId":"test2","Success":true,"Status":"CONFIRMED",
             "PaymentId":"2006896","ErrorCode":"0","Amount":102120,
             "SomeFieldTheBankAddedLater":"matters","Token":"{token}"}
            """;

        var signed = new SortedDictionary<string, string>(StringComparer.Ordinal)
        {
            ["Amount"] = "102120",
            ["ErrorCode"] = "0",
            ["OrderId"] = "test2",
            ["PaymentId"] = "2006896",
            ["Password"] = password,
            ["SomeFieldTheBankAddedLater"] = "matters",
            ["Status"] = "CONFIRMED",
            ["Success"] = "true",
            ["TerminalKey"] = "1510572937960"
        };
        var token = Sha256Hex(string.Concat(signed.Values));

        var notification = JsonSerializer.Deserialize<TBankPaymentNotification>(json.Replace("{token}", token))!;

        Assert.Equal("matters", notification.AdditionalFields?["SomeFieldTheBankAddedLater"].GetString());
        Assert.Equal(
            TBankPaymentNotificationValidationResult.Valid,
            TBankPaymentNotificationValidator.ValidateToken(notification, password));
    }

    // DATA — вложенный объект, и банк его не подписывает. Раньше поле было строкой: такое тело не
    // разбиралось вовсе.
    [Fact]
    public void ValidateToken_LeavesTheNestedDataObjectOutOfTheSignature()
    {
        const string password = "Dfsfh56dgKl";
        const string json = """
            {"TerminalKey":"1510572937960","OrderId":"test2","Success":true,"Status":"CONFIRMED",
             "PaymentId":"2006896","ErrorCode":"0","Amount":102120,
             "DATA":{"Phone":"+79031234567","Email":"a@b.c"},"Token":"{token}"}
            """;

        var signed = new SortedDictionary<string, string>(StringComparer.Ordinal)
        {
            ["Amount"] = "102120",
            ["ErrorCode"] = "0",
            ["OrderId"] = "test2",
            ["Password"] = password,
            ["PaymentId"] = "2006896",
            ["Status"] = "CONFIRMED",
            ["Success"] = "true",
            ["TerminalKey"] = "1510572937960"
        };
        var token = Sha256Hex(string.Concat(signed.Values));

        var notification = JsonSerializer.Deserialize<TBankPaymentNotification>(json.Replace("{token}", token))!;

        Assert.Equal("""{"Phone":"+79031234567","Email":"a@b.c"}""", notification.DATA);
        Assert.Equal(
            TBankPaymentNotificationValidationResult.Valid,
            TBankPaymentNotificationValidator.ValidateToken(notification, password));
    }

    [Fact]
    public void ValidateToken_DeserializesTheDealIdFromAMultisplitNotification()
    {
        const string json = """
            {"TerminalKey":"1775646908452","OrderId":"SGH-20260926092454019","Success":true,
             "Status":"CONFIRMED","PaymentId":9319671978,"ErrorCode":"0","Amount":5000,
             "CardId":707293266,"Pan":"T-Pay_account","Token":"t","SpAccumulationId":"91954170"}
            """;

        var notification = JsonSerializer.Deserialize<TBankPaymentNotification>(json)!;

        Assert.Equal("91954170", notification.SpAccumulationId);
        Assert.Equal("9319671978", notification.PaymentId);
        Assert.Null(notification.AdditionalFields);
    }

    // Единственный надёжный путь: подпись считается по проводу, а не по разобранной модели.
    [Fact]
    public void TryRead_ValidatesFromTheBodyAsItArrived()
    {
        const string password = "Dfsfh56dgKl";
        var body = SignedBody(password, ("TerminalKey", "1510572937960"), ("OrderId", "test2"),
            ("Success", "true"), ("Status", "CONFIRMED"), ("PaymentId", "2006896"),
            ("ErrorCode", "0"), ("Amount", "102120"));

        var result = TBankPaymentNotificationValidator.TryRead(body, password, out var notification);

        Assert.Equal(TBankPaymentNotificationValidationResult.Valid, result);
        Assert.NotNull(notification);
        Assert.Equal("test2", notification!.OrderId);
        Assert.Equal(TBankPaymentStatus.CONFIRMED, notification.Status);
        Assert.Equal("CONFIRMED", notification.StatusRaw);
        Assert.Equal("102120", notification.RawFields!["Amount"]);
        Assert.DoesNotContain("Token", notification.RawFields!.Keys);
    }

    // Банк вводит новый статус — событие о деньгах терять нельзя. Раньше разбор падал целиком.
    [Fact]
    public void TryRead_KeepsANotificationWhoseStatusTheSdkDoesNotKnow()
    {
        const string password = "Dfsfh56dgKl";
        var body = SignedBody(password, ("TerminalKey", "1510572937960"), ("OrderId", "test2"),
            ("Status", "SOME_STATUS_SHIPPED_LATER"), ("PaymentId", "2006896"),
            ("ErrorCode", "0"), ("Amount", "102120"));

        var result = TBankPaymentNotificationValidator.TryRead(body, password, out var notification);

        Assert.Equal(TBankPaymentNotificationValidationResult.Valid, result);
        Assert.Equal(TBankPaymentStatus.UNKNOWN, notification!.Status);
        Assert.Equal("SOME_STATUS_SHIPPED_LATER", notification.StatusRaw);
    }

    // Строгий разбор ответа на запрос остаётся строгим: там непонятный статус — это пробел в SDK,
    // и молчать о нём нельзя. Терпимость добавлена только нотификации.
    [Fact]
    public void PaymentStateResponse_StillRefusesAnUnknownStatus()
    {
        Assert.Throws<NotImplementedException>(() => JsonSerializer.Deserialize<TBankPaymentStateResponse>(
            """{"TerminalKey":"T","OrderId":"1","Success":true,"Status":"SOME_STATUS_SHIPPED_LATER","PaymentId":"2","ErrorCode":"0"}"""));
    }

    [Fact]
    public void TryRead_SignsAFieldTheModelDoesNotDeclare()
    {
        const string password = "Dfsfh56dgKl";
        var body = SignedBody(password, ("TerminalKey", "1510572937960"), ("OrderId", "test2"),
            ("Status", "CONFIRMED"), ("PaymentId", "2006896"), ("ErrorCode", "0"),
            ("Amount", "102120"), ("SomeFieldTheBankAddedLater", "matters"));

        var result = TBankPaymentNotificationValidator.TryRead(body, password, out var notification);

        Assert.Equal(TBankPaymentNotificationValidationResult.Valid, result);
        Assert.Equal("matters", notification!.RawFields!["SomeFieldTheBankAddedLater"]);
    }

    [Fact]
    public void TryRead_LeavesTheNestedDataObjectOutOfTheSignature()
    {
        const string password = "Dfsfh56dgKl";
        var signed = SignedBody(password, ("TerminalKey", "1510572937960"), ("OrderId", "test2"),
            ("Status", "CONFIRMED"), ("PaymentId", "2006896"), ("ErrorCode", "0"), ("Amount", "102120"));
        var body = signed.Insert(1, "\"DATA\":{\"Phone\":\"+79031234567\"},");

        var result = TBankPaymentNotificationValidator.TryRead(body, password, out var notification);

        Assert.Equal(TBankPaymentNotificationValidationResult.Valid, result);
        Assert.Equal("""{"Phone":"+79031234567"}""", notification!.DATA);
        Assert.DoesNotContain("DATA", notification.RawFields!.Keys);
    }

    [Fact]
    public void TryRead_ReportsASignatureThatDoesNotMatch()
    {
        var body = SignedBody("a-different-password", ("TerminalKey", "T"), ("OrderId", "1"),
            ("Status", "CONFIRMED"), ("PaymentId", "2"), ("ErrorCode", "0"));

        Assert.Equal(
            TBankPaymentNotificationValidationResult.InvalidToken,
            TBankPaymentNotificationValidator.TryRead(body, "Dfsfh56dgKl", out _));
    }

    [Theory]
    [InlineData("")]
    [InlineData("not json")]
    [InlineData("[1,2,3]")]
    [InlineData("\"just a string\"")]
    public void TryRead_ReportsABodyThatIsNotANotification(string body)
    {
        Assert.Equal(
            TBankPaymentNotificationValidationResult.MalformedBody,
            TBankPaymentNotificationValidator.TryRead(body, "Dfsfh56dgKl", out var notification));
        Assert.Null(notification);
    }

    [Fact]
    public void TryRead_ReportsAMissingToken()
    {
        Assert.Equal(
            TBankPaymentNotificationValidationResult.MissingToken,
            TBankPaymentNotificationValidator.TryRead(
                """{"TerminalKey":"T","OrderId":"1","Status":"CONFIRMED","PaymentId":"2","ErrorCode":"0"}""",
                "Dfsfh56dgKl",
                out _));
    }

    /// <summary>Тело нотификации, подписанное так, как его подписал бы банк.</summary>
    private static string SignedBody(string password, params (string Key, string Value)[] fields)
    {
        var signed = new SortedDictionary<string, string>(StringComparer.Ordinal) { ["Password"] = password };
        foreach (var field in fields)
        {
            signed[field.Key] = field.Value;
        }

        var token = Sha256Hex(string.Concat(signed.Values));
        var body = new System.Text.StringBuilder("{");
        foreach (var field in fields)
        {
            body.Append(JsonSerializer.Serialize(field.Key)).Append(':');
            body.Append(field.Key is "Amount" or "Success" ? field.Value : JsonSerializer.Serialize(field.Value));
            body.Append(',');
        }

        return body.Append("\"Token\":\"").Append(token).Append("\"}").ToString();
    }

    private static string Sha256Hex(string input)
    {
        return Convert.ToHexString(
                System.Security.Cryptography.SHA256.HashData(System.Text.Encoding.UTF8.GetBytes(input)))
            .ToLowerInvariant();
    }
}
