namespace TBankAcquiringNet;

/// <summary>
/// Параметры отрисовки QR-кода.
/// </summary>
/// <remarks>
/// Значения по умолчанию рассчитаны на платёжную ссылку, показанную на экране: уровень коррекции
/// <see cref="TBankQrEccLevel.M"/>, 8 пикселей на модуль и обязательная по стандарту светлая
/// рамка в 4 модуля. Рамку можно уменьшить, но сканеры опираются на неё, чтобы найти символ,
/// поэтому ноль ставить не стоит.
/// </remarks>
public sealed class TBankQrRenderOptions
{
    /// <summary>
    /// Уровень коррекции ошибок. По умолчанию <see cref="TBankQrEccLevel.M"/>.
    /// </summary>
    public TBankQrEccLevel EccLevel { get; set; } = TBankQrEccLevel.M;

    /// <summary>
    /// Размер одного модуля в пикселях для растровой отрисовки и в единицах SVG — для векторной.
    /// По умолчанию 8.
    /// </summary>
    public int ModuleSize { get; set; } = 8;

    /// <summary>
    /// Ширина светлой рамки вокруг символа, в модулях. По умолчанию 4, как требует стандарт.
    /// </summary>
    public int QuietZone { get; set; } = 4;

    /// <summary>
    /// Цвет тёмных модулей в формате CSS. По умолчанию <c>#000000</c>. Применяется только к SVG;
    /// PNG всегда чёрно-белый.
    /// </summary>
    public string DarkColor { get; set; } = "#000000";

    /// <summary>
    /// Цвет фона в формате CSS. По умолчанию <c>#ffffff</c>. Значение <see langword="null"/>
    /// оставляет фон прозрачным. Применяется только к SVG.
    /// </summary>
    public string? LightColor { get; set; } = "#ffffff";

    internal void Validate()
    {
        if (ModuleSize < 1)
        {
            throw new ArgumentOutOfRangeException(
                nameof(ModuleSize), ModuleSize, "Размер модуля должен быть не меньше 1.");
        }

        if (QuietZone < 0)
        {
            throw new ArgumentOutOfRangeException(
                nameof(QuietZone), QuietZone, "Ширина рамки не может быть отрицательной.");
        }

        if (EccLevel is < TBankQrEccLevel.L or > TBankQrEccLevel.H)
        {
            throw new ArgumentOutOfRangeException(
                nameof(EccLevel), EccLevel, "Неизвестный уровень коррекции ошибок.");
        }
    }
}
