namespace TBankAcquiringNet;

/// <summary>
/// Ключи объекта <c>DATA</c> метода Init для кошельков T‑Pay и SberPay.
/// Значения передаются через <see cref="TBankInitPaymentRequest.DATA"/>.
/// </summary>
/// <remarks>
/// Сценарии: <see href="https://developer.tbank.ru/eacq/scenarios/payments/PCI_DSS/t-pay/">T‑Pay</see>,
/// <see href="https://developer.tbank.ru/eacq/scenarios/payments/PCI_DSS/sberpay/">SberPay</see>.
/// </remarks>
public static class TBankInitDataKeys
{
    /// <summary>Использовать платежную форму мерчанта для SberPay Web. Значение — "true".</summary>
    public const string SberPayWeb = "SberPayWeb";

    /// <summary>Использовать платежную форму мерчанта для T‑Pay Web. Значение — "true".</summary>
    public const string TinkoffPayWeb = "TinkoffPayWeb";

    /// <summary>Тип устройства покупателя (см. <see cref="TBankDeviceType"/>).</summary>
    public const string Device = "Device";

    /// <summary>ОС устройства покупателя (см. <see cref="TBankDeviceOs"/>).</summary>
    public const string DeviceOs = "DeviceOs";

    /// <summary>Признак открытия во встроенном webview. Значение — "true".</summary>
    public const string DeviceWebView = "DeviceWebView";
}

/// <summary>
/// Значения ключа <see cref="TBankInitDataKeys.Device"/>.
/// </summary>
public static class TBankDeviceType
{
    /// <summary>Оплата с мобильного устройства.</summary>
    public const string Mobile = "Mobile";

    /// <summary>Оплата с десктопа.</summary>
    public const string Desktop = "Desktop";
}

/// <summary>
/// Значения ключа <see cref="TBankInitDataKeys.DeviceOs"/>.
/// </summary>
public static class TBankDeviceOs
{
    /// <summary>iOS.</summary>
    public const string Ios = "iOS";

    /// <summary>Android.</summary>
    public const string Android = "Android";

    /// <summary>macOS.</summary>
    public const string MacOs = "macOS";

    /// <summary>Windows.</summary>
    public const string Windows = "Windows";

    /// <summary>Linux.</summary>
    public const string Linux = "Linux";
}
