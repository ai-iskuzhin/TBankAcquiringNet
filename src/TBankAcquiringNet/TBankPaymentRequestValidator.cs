namespace TBankAcquiringNet;

internal static class TBankPaymentRequestValidator
{
    public static void Validate(TBankInitPaymentRequest request)
    {
        RequirePositiveAmount(request.Amount, nameof(request.Amount));
        RequireMinimumAmount(request.Amount, 100, nameof(request.Amount));
        RequireText(request.OrderId, nameof(request.OrderId));

        if (request.DATA is { Count: > 20 })
        {
            throw new TBankAcquiringValidationException("DATA cannot contain more than 20 pairs.");
        }
    }

    public static void Validate(TBankPaymentStateRequest request)
    {
        RequireText(request.PaymentId, nameof(request.PaymentId));
    }

    public static void Validate(TBankCheckOrderRequest request)
    {
        RequireText(request.OrderId, nameof(request.OrderId));
    }

    public static void Validate(TBankCancelPaymentRequest request)
    {
        RequireText(request.PaymentId, nameof(request.PaymentId));

        if (request.Amount is { } amount)
        {
            RequirePositiveAmount(amount, nameof(request.Amount));
            RequireMinimumAmount(amount, 100, nameof(request.Amount));
        }
    }

    public static void Validate(TBankConfirmPaymentRequest request)
    {
        RequireText(request.PaymentId, nameof(request.PaymentId));

        if (request.Amount is { } amount)
        {
            RequirePositiveAmount(amount, nameof(request.Amount));
            RequireMinimumAmount(amount, 100, nameof(request.Amount));
        }
    }

    public static void Validate(TBankQrRequest request)
    {
        RequireText(request.PaymentId, nameof(request.PaymentId));
    }

    public static void Validate(TBankChargeQrRequest request)
    {
        RequireText(request.PaymentId, nameof(request.PaymentId));
        RequireText(request.AccountToken, nameof(request.AccountToken));

        if (request.SendEmail == true)
        {
            RequireText(request.InfoEmail, nameof(request.InfoEmail));
        }
    }

    private static void RequireText(string? value, string parameterName)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            throw new TBankAcquiringValidationException($"{parameterName} is required.");
        }
    }

    private static void RequirePositiveAmount(TBankAmount amount, string parameterName)
    {
        if (amount.MinorUnits <= 0)
        {
            throw new TBankAcquiringValidationException($"{parameterName} must be greater than zero.");
        }
    }

    private static void RequireMinimumAmount(TBankAmount amount, long minimumMinorUnits, string parameterName)
    {
        if (amount.MinorUnits < minimumMinorUnits)
        {
            throw new TBankAcquiringValidationException($"{parameterName} must be at least {minimumMinorUnits} minor units.");
        }
    }
}
