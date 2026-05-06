namespace TBankAcquiringNet.Payments;

/// <summary>
/// Формат данных, возвращаемых методом GetQr.
/// </summary>
public enum TBankQrDataType
{
    /// <summary>Вернуть payload QR-кода.</summary>
    Payload,

    /// <summary>Вернуть SVG-изображение QR-кода.</summary>
    Image
}
