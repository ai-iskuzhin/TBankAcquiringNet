using TBankAcquiringNet.Payments;

namespace TBankAcquiringNet.Payments.Tests;

public sealed class TBankPaymentErrorCodesTests
{
    [Fact]
    public void IsSuccess_ReturnsTrueOnlyForSuccessCode()
    {
        Assert.True(TBankPaymentErrorCodes.IsSuccess("0"));
        Assert.False(TBankPaymentErrorCodes.IsSuccess("1051"));
        Assert.False(TBankPaymentErrorCodes.IsSuccess(null));
    }

    [Theory]
    [InlineData(TBankPaymentErrorCodes.ForeignCardUnavailable, "иностранной карте")]
    [InlineData(TBankPaymentErrorCodes.InsufficientFunds, "Недостаточно средств")]
    [InlineData(TBankPaymentErrorCodes.QrPayUnavailable, "QrPay")]
    [InlineData(TBankPaymentErrorCodes.AccountBindingNotFound, "Привязка счета")]
    [InlineData(TBankPaymentErrorCodes.RecurrentPaymentsUnavailable, "Рекуррентные платежи")]
    [InlineData(TBankPaymentErrorCodes.InvalidAccountTokenStatus, "AccountToken")]
    public void GetDescription_ReturnsKnownRussianDescription(string errorCode, string expectedText)
    {
        var description = TBankPaymentErrorCodes.GetDescription(errorCode);

        Assert.NotNull(description);
        Assert.Contains(expectedText, description);
    }

    [Fact]
    public void TryGetDescription_ReturnsFalseForUnknownCode()
    {
        var found = TBankPaymentErrorCodes.TryGetDescription("unknown", out var description);

        Assert.False(found);
        Assert.Equal(string.Empty, description);
    }
}
