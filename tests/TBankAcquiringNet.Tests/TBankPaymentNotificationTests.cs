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
}
