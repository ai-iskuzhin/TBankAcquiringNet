namespace TBankAcquiringNet.Payments;

internal static class TBankWireNames
{
    public static string FormatPaymentStatus(TBankPaymentStatus value)
    {
        return value switch
        {
            TBankPaymentStatus.New => "NEW",
            TBankPaymentStatus.Canceled => "CANCELED",
            TBankPaymentStatus.Preauthorizing => "PREAUTHORIZING",
            TBankPaymentStatus.FormShowed => "FORM_SHOWED",
            TBankPaymentStatus.Authorizing => "AUTHORIZING",
            TBankPaymentStatus.ThreeDsChecking => "3DS_CHECKING",
            TBankPaymentStatus.ThreeDsChecked => "3DS_CHECKED",
            TBankPaymentStatus.Authorized => "AUTHORIZED",
            TBankPaymentStatus.PayChecking => "PAY_CHECKING",
            TBankPaymentStatus.Confirming => "CONFIRMING",
            TBankPaymentStatus.ConfirmChecking => "CONFIRM_CHECKING",
            TBankPaymentStatus.Confirmed => "CONFIRMED",
            TBankPaymentStatus.Reversing => "REVERSING",
            TBankPaymentStatus.PartialReversed => "PARTIAL_REVERSED",
            TBankPaymentStatus.Reversed => "REVERSED",
            TBankPaymentStatus.Refunding => "REFUNDING",
            TBankPaymentStatus.AsyncRefunding => "ASYNC_REFUNDING",
            TBankPaymentStatus.PartialRefunded => "PARTIAL_REFUNDED",
            TBankPaymentStatus.Refunded => "REFUNDED",
            TBankPaymentStatus.DeadlineExpired => "DEADLINE_EXPIRED",
            TBankPaymentStatus.Rejected => "REJECTED",
            TBankPaymentStatus.AuthFail => "AUTH_FAIL",
            _ => "UNKNOWN"
        };
    }

    public static string FormatQrDataType(TBankQrDataType value)
    {
        return value switch
        {
            TBankQrDataType.Payload => "PAYLOAD",
            TBankQrDataType.Image => "IMAGE",
            _ => throw new InvalidOperationException("Unknown T-Bank QR data type.")
        };
    }
}
