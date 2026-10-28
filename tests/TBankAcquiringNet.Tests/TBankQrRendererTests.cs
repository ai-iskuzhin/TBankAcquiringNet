using System.IO.Compression;
using System.Text;
using Xunit;

namespace TBankAcquiringNet.Tests;

/// <summary>
/// Отрисовка закодированного QR-кода в SVG и PNG.
/// </summary>
/// <remarks>
/// Сам код проверяется в <see cref="TBankQrCodeTests"/> сверкой с эталоном; здесь — что
/// картинка повторяет матрицу один в один. Для PNG это настоящий обратный ход: поток
/// распаковывается и строки пикселей сравниваются с модулями, иначе ошибка в упаковке битов
/// или в контрольной сумме прошла бы незамеченной — файл всё равно открылся бы.
/// </remarks>
public class TBankQrRendererTests
{
    private const string TpayLink =
        "https://tpay.tbank-online.com/check/?productRequestId=01M61G1E419T4PNW7XT77J2R83";

    private const string SberPayLink =
        "sberpay://invoicing/v2?bankInvoiceId=01a16ac956d6594b165f9b8df797a643&operationType=Web2App";

    [Fact]
    public void Svg_covers_the_whole_symbol_and_nothing_else()
    {
        var options = new TBankQrRenderOptions { QuietZone = 4, ModuleSize = 8 };
        var code = TBankQrCode.Encode(TpayLink, options.EccLevel);

        var svg = TBankQrRenderer.ToSvg(TpayLink, options);

        var span = code.Size + 8;
        Assert.Contains($"viewBox=\"0 0 {span} {span}\"", svg);
        Assert.Contains($"width=\"{span * 8}\"", svg);
        Assert.EndsWith("</svg>", svg);

        // Каждый тёмный модуль попал в путь ровно один раз: горизонтальные серии
        // склеиваются, поэтому считаем суммарную длину, а не число команд.
        var drawn = 0;
        foreach (var segment in svg.Split('M'))
        {
            var h = segment.IndexOf('h');
            var v = segment.IndexOf('v');
            if (h > 0 && v > h)
            {
                drawn += int.Parse(segment.Substring(h + 1, v - h - 1));
            }
        }

        var dark = 0;
        for (var row = 0; row < code.Size; row++)
        {
            for (var col = 0; col < code.Size; col++)
            {
                if (code.Modules[row, col])
                {
                    dark++;
                }
            }
        }

        Assert.Equal(dark, drawn);
    }

    [Fact]
    public void Svg_leaves_the_background_out_when_asked()
    {
        var transparent = TBankQrRenderer.ToSvg(TpayLink, new TBankQrRenderOptions { LightColor = null });
        var filled = TBankQrRenderer.ToSvg(TpayLink, new TBankQrRenderOptions { LightColor = "#fff" });

        Assert.DoesNotContain("<rect", transparent);
        Assert.Contains("<rect", filled);
    }

    [Theory]
    [InlineData(1)]
    [InlineData(3)]
    [InlineData(8)]
    public void Png_pixels_repeat_the_matrix(int moduleSize)
    {
        var options = new TBankQrRenderOptions { ModuleSize = moduleSize, QuietZone = 2 };
        var code = TBankQrCode.Encode(SberPayLink, options.EccLevel);

        var png = TBankQrRenderer.ToPng(SberPayLink, options);

        var span = code.Size + (options.QuietZone * 2);
        var pixels = span * moduleSize;
        Assert.Equal(pixels, ReadIhdrWidth(png));

        var raw = InflateIdat(png);
        var stride = (pixels + 7) / 8;
        Assert.Equal((stride + 1) * pixels, raw.Length);

        for (var y = 0; y < pixels; y++)
        {
            var offset = y * (stride + 1);
            Assert.Equal(0, raw[offset]); // фильтр строки
            for (var x = 0; x < pixels; x++)
            {
                var white = (raw[offset + 1 + (x / 8)] & (0x80 >> (x % 8))) != 0;
                var row = (y / moduleSize) - options.QuietZone;
                var col = (x / moduleSize) - options.QuietZone;
                var dark = row >= 0 && row < code.Size && col >= 0 && col < code.Size
                    && code.Modules[row, col];

                Assert.Equal(dark, !white);
            }
        }
    }

    [Fact]
    public void Png_starts_with_the_signature_and_ends_with_iend()
    {
        var png = TBankQrRenderer.ToPng(TpayLink);

        Assert.Equal<byte[]>([0x89, 0x50, 0x4E, 0x47, 0x0D, 0x0A, 0x1A, 0x0A], png.Take(8).ToArray());
        Assert.Equal("IEND", Encoding.ASCII.GetString(png, png.Length - 8, 4));
    }

    [Fact]
    public void Base64_helpers_wrap_the_same_bytes()
    {
        Assert.Equal(
            Convert.ToBase64String(Encoding.UTF8.GetBytes(TBankQrRenderer.ToSvg(TpayLink))),
            TBankQrRenderer.ToSvgBase64(TpayLink));
        Assert.Equal(
            Convert.ToBase64String(TBankQrRenderer.ToPng(TpayLink)),
            TBankQrRenderer.ToPngBase64(TpayLink));
        Assert.StartsWith("data:image/svg+xml;base64,", TBankQrRenderer.ToSvgDataUri(TpayLink));
        Assert.StartsWith("data:image/png;base64,", TBankQrRenderer.ToPngDataUri(TpayLink));
    }

    [Fact]
    public void A_deeplink_is_reported_as_not_scannable()
    {
        // Ради этого различия метод и существует: QR с «sberpay://» штатная камера не откроет,
        // поэтому вызывающий код должен знать об этом до того, как покажет код покупателю.
        Assert.True(TBankQrRenderer.IsLikelyScannable(TpayLink));
        Assert.True(TBankQrRenderer.IsLikelyScannable("http://example.test/pay"));
        Assert.False(TBankQrRenderer.IsLikelyScannable(SberPayLink));
        Assert.False(TBankQrRenderer.IsLikelyScannable("bank100000000004://Main/tpay/4000000000053423585"));
        Assert.False(TBankQrRenderer.IsLikelyScannable("не ссылка"));
    }

    [Fact]
    public void Empty_and_broken_input_is_refused()
    {
        Assert.Throws<ArgumentNullException>(() => TBankQrRenderer.ToSvg(null!));
        Assert.Throws<ArgumentException>(() => TBankQrRenderer.ToSvg(string.Empty));
        Assert.Throws<ArgumentOutOfRangeException>(
            () => TBankQrRenderer.ToSvg(TpayLink, new TBankQrRenderOptions { ModuleSize = 0 }));
        Assert.Throws<ArgumentOutOfRangeException>(
            () => TBankQrRenderer.ToSvg(TpayLink, new TBankQrRenderOptions { QuietZone = -1 }));
    }

    [Fact]
    public void Data_that_outgrows_version_40_is_refused_rather_than_truncated()
    {
        // Молча обрезать ссылку нельзя: код отрисуется, отсканируется и приведёт не туда.
        var tooLong = new string('A', 5000);

        Assert.Throws<ArgumentException>(() => TBankQrRenderer.ToSvg(tooLong));
    }

    private static int ReadIhdrWidth(byte[] png) =>
        (png[16] << 24) | (png[17] << 16) | (png[18] << 8) | png[19];

    /// <summary>Достаёт и распаковывает содержимое IDAT, снимая обёртку zlib.</summary>
    private static byte[] InflateIdat(byte[] png)
    {
        var offset = 8;
        var idat = new MemoryStream();
        while (offset < png.Length)
        {
            var length = (png[offset] << 24) | (png[offset + 1] << 16)
                | (png[offset + 2] << 8) | png[offset + 3];
            var type = Encoding.ASCII.GetString(png, offset + 4, 4);
            if (type == "IDAT")
            {
                // Первые два байта — заголовок zlib, их DeflateStream не ест.
                idat.Write(png, offset + 8 + 2, length - 2);
            }

            offset += 12 + length;
        }

        idat.Position = 0;
        using var inflate = new DeflateStream(idat, CompressionMode.Decompress);
        using var raw = new MemoryStream();
        inflate.CopyTo(raw);

        return raw.ToArray();
    }
}
