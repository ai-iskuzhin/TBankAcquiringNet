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

    public static void Validate(TBankQrStateRequest request)
    {
        RequireText(request.PaymentId, nameof(request.PaymentId));
    }

    public static void Validate(TBankQrBankListRequest request)
    {
        if (request.Device is null)
        {
            throw new TBankAcquiringValidationException($"{nameof(request.Device)} is required.");
        }

        RequireText(request.Device.Type, $"{nameof(request.Device)}.{nameof(request.Device.Type)}");
        RequireText(request.Device.Os, $"{nameof(request.Device)}.{nameof(request.Device.Os)}");
    }

    public static void Validate(TBankAccountQrListRequest request)
    {
        // TerminalKey is populated by the client before signing; nothing else to validate.
        _ = request;
    }

    public static void Validate(TBankAddAccountQrStateRequest request)
    {
        RequireText(request.RequestKey, nameof(request.RequestKey));
    }

    public static void Validate(TBankAddAccountQrRequest request)
    {
        RequireText(request.Description, nameof(request.Description));

        if (request.Data is { Count: > 20 })
        {
            throw new TBankAcquiringValidationException("Data cannot contain more than 20 pairs.");
        }
    }

    public static void Validate(TBankQrMembersListRequest request)
    {
        RequireText(request.PaymentId, nameof(request.PaymentId));
    }

    public static void Validate(TBankMirPayDeepLinkRequest request)
    {
        RequireText(request.PaymentId, nameof(request.PaymentId));
    }

    public static void Validate(TBankAlfaPayLinkRequest request)
    {
        RequireText(request.PaymentId, nameof(request.PaymentId));
    }

    public static void Validate(TBankSendClosingReceiptFfd12Request request)
    {
        RequireText(request.PaymentId, nameof(request.PaymentId));
        RequireReceiptItems(request.Receipt, request.Receipt?.Items?.Count);
    }

    public static void Validate(TBankSendClosingReceiptFfd105Request request)
    {
        RequireText(request.PaymentId, nameof(request.PaymentId));
        RequireReceiptItems(request.Receipt, request.Receipt?.Items?.Count);
    }

    private static void RequireReceiptItems(object? receipt, int? itemCount)
    {
        if (receipt is null)
        {
            throw new TBankAcquiringValidationException("Receipt is required.");
        }

        if (itemCount is not > 0)
        {
            throw new TBankAcquiringValidationException("Receipt.Items must contain at least one item.");
        }

        if (itemCount > 100)
        {
            throw new TBankAcquiringValidationException("Receipt.Items cannot contain more than 100 items.");
        }
    }

    public static void Validate(TBankSbpPayTestRequest request)
    {
        RequireText(request.PaymentId, nameof(request.PaymentId));

        if (request.IsDeadlineExpired == true && request.IsRejected == true)
        {
            throw new TBankAcquiringValidationException(
                "IsDeadlineExpired and IsRejected cannot both be true.");
        }
    }

    public static void Validate(TBankAddCustomerRequest request) => RequireText(request.CustomerKey, nameof(request.CustomerKey));

    public static void Validate(TBankGetCustomerRequest request) => RequireText(request.CustomerKey, nameof(request.CustomerKey));

    public static void Validate(TBankRemoveCustomerRequest request) => RequireText(request.CustomerKey, nameof(request.CustomerKey));

    public static void Validate(TBankAddCardRequest request) => RequireText(request.CustomerKey, nameof(request.CustomerKey));

    public static void Validate(TBankGetAddCardStateRequest request) => RequireText(request.RequestKey, nameof(request.RequestKey));

    public static void Validate(TBankGetCardListRequest request) => RequireText(request.CustomerKey, nameof(request.CustomerKey));

    public static void Validate(TBankRemoveCardRequest request)
    {
        RequireText(request.CustomerKey, nameof(request.CustomerKey));
        RequireText(request.CardId, nameof(request.CardId));
    }

    public static void Validate(TBankChargeRequest request)
    {
        RequireText(request.PaymentId, nameof(request.PaymentId));
        RequireText(request.RebillId, nameof(request.RebillId));

        if (request.SendEmail == true)
        {
            RequireText(request.InfoEmail, nameof(request.InfoEmail));
        }
    }

    public static void Validate(TBankCheck3dsVersionRequest request)
    {
        RequireText(request.PaymentId, nameof(request.PaymentId));
        RequireText(request.CardData, nameof(request.CardData));
    }

    public static void Validate(TBankAttachCardRequest request)
    {
        RequireText(request.RequestKey, nameof(request.RequestKey));
        RequireText(request.CardData, nameof(request.CardData));

        if (request.DATA is { Count: > 20 })
        {
            throw new TBankAcquiringValidationException("DATA cannot contain more than 20 pairs.");
        }
    }

    public static void Validate(TBankSubmit3DSAuthorizationRequest request)
    {
        RequireText(request.MD, nameof(request.MD));
        RequireText(request.PaRes, nameof(request.PaRes));
    }

    public static void Validate(TBankSubmit3DSAuthorizationV2Request request)
    {
        RequireText(request.PaymentId, nameof(request.PaymentId));
    }

    public static void Validate(TBankGetConfirmOperationRequest request)
    {
        var hasCallbackUrl = !string.IsNullOrWhiteSpace(request.CallbackUrl);
        var hasEmailList = request.EmailList is { Count: > 0 };

        if (hasCallbackUrl == hasEmailList)
        {
            throw new TBankAcquiringValidationException(
                $"Exactly one of {nameof(request.CallbackUrl)} or {nameof(request.EmailList)} is required.");
        }

        if (hasEmailList)
        {
            foreach (var recipient in request.EmailList!)
            {
                RequireText(recipient?.Email, $"{nameof(request.EmailList)}.{nameof(TBankConfirmOperationEmail.Email)}");
            }
        }

        if (request.PaymentIdList is not { Count: > 0 })
        {
            throw new TBankAcquiringValidationException($"{nameof(request.PaymentIdList)} must contain at least one payment id.");
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
