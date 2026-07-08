namespace TBankAcquiringNet;

internal static class TBankWireNames
{
    public static string FormatPaymentStatus(TBankPaymentStatus value)
    {
        return value switch
        {
            TBankPaymentStatus.NEW => "NEW",
            TBankPaymentStatus.CANCELED => "CANCELED",
            TBankPaymentStatus.PREAUTHORIZING => "PREAUTHORIZING",
            TBankPaymentStatus.FORM_SHOWED => "FORM_SHOWED",
            TBankPaymentStatus.AUTHORIZING => "AUTHORIZING",
            TBankPaymentStatus.THREE_DS_CHECKING => "3DS_CHECKING",
            TBankPaymentStatus.THREE_DS_CHECKED => "3DS_CHECKED",
            TBankPaymentStatus.AUTHORIZED => "AUTHORIZED",
            TBankPaymentStatus.PAY_CHECKING => "PAY_CHECKING",
            TBankPaymentStatus.CONFIRMING => "CONFIRMING",
            TBankPaymentStatus.CONFIRM_CHECKING => "CONFIRM_CHECKING",
            TBankPaymentStatus.CONFIRMED => "CONFIRMED",
            TBankPaymentStatus.REVERSING => "REVERSING",
            TBankPaymentStatus.PARTIAL_REVERSED => "PARTIAL_REVERSED",
            TBankPaymentStatus.REVERSED => "REVERSED",
            TBankPaymentStatus.REFUNDING => "REFUNDING",
            TBankPaymentStatus.ASYNC_REFUNDING => "ASYNC_REFUNDING",
            TBankPaymentStatus.PARTIAL_REFUNDED => "PARTIAL_REFUNDED",
            TBankPaymentStatus.REFUNDED => "REFUNDED",
            TBankPaymentStatus.DEADLINE_EXPIRED => "DEADLINE_EXPIRED",
            TBankPaymentStatus.REJECTED => "REJECTED",
            TBankPaymentStatus.AUTH_FAIL => "AUTH_FAIL",
            TBankPaymentStatus.CANCEL_CHECKING => "CANCEL_CHECKING",
            TBankPaymentStatus.CHECKING => "CHECKING",
            TBankPaymentStatus.CHECKED => "CHECKED",
            TBankPaymentStatus.COMPLETING => "COMPLETING",
            TBankPaymentStatus.COMPLETED => "COMPLETED",
            TBankPaymentStatus.PROCESSING => "PROCESSING",
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

    public static string FormatPayType(TBankPayType value)
    {
        return value switch
        {
            TBankPayType.OneStage => "O",
            TBankPayType.TwoStage => "T",
            _ => throw new InvalidOperationException("Unknown T-Bank pay type.")
        };
    }

    public static string FormatLanguage(TBankLanguage value)
    {
        return value switch
        {
            TBankLanguage.Ru => "ru",
            TBankLanguage.En => "en",
            _ => throw new InvalidOperationException("Unknown T-Bank language.")
        };
    }

    public static string FormatRecurrent(TBankRecurrent value)
    {
        return value switch
        {
            TBankRecurrent.Yes => "Y",
            _ => throw new InvalidOperationException("Unknown T-Bank recurrent flag.")
        };
    }

    public static string FormatAccountQrStatus(TBankAccountQrStatus value)
    {
        return value switch
        {
            TBankAccountQrStatus.NEW => "NEW",
            TBankAccountQrStatus.PROCESSING => "PROCESSING",
            TBankAccountQrStatus.ACTIVE => "ACTIVE",
            TBankAccountQrStatus.INACTIVE => "INACTIVE",
            _ => "UNKNOWN"
        };
    }
}
