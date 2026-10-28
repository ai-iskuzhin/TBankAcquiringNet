using System.Globalization;
using System.IO.Compression;
using System.Text;

namespace TBankAcquiringNet;

/// <summary>
/// Рисует QR-код по строке — обычно по платёжной ссылке, полученной от T-Bank.
/// </summary>
/// <remarks>
/// Нужен там, где покупатель платит с компьютера: ссылку T-Pay или SberPay на телефон иначе не
/// передать. Раньше картинку отдавал сам банк методами <c>TinkoffPay QR</c> и <c>SberPay QR</c>,
/// но они выведены из эксплуатации и отвечают <c>500</c> — банк рекомендует брать ссылку
/// методами <c>/link</c> и показывать её самостоятельно. Этот класс и есть вторая половина:
/// ссылка приходит по сети, код рисуется на месте.
/// <para>
/// Важно, какую ссылку кодировать. У T-Pay <c>RedirectUrl</c> — обычный адрес <c>https</c>,
/// его камера телефона откроет без вопросов. У SberPay это <c>sberpay://…</c>: такую схему
/// штатное приложение «Камера» обычно не открывает, поэтому QR со ссылкой SberPay помогает
/// далеко не всем. Проверить можно через <see cref="IsLikelyScannable"/>.
/// </para>
/// </remarks>
public static class TBankQrRenderer
{
    private static readonly byte[] PngSignature = [0x89, 0x50, 0x4E, 0x47, 0x0D, 0x0A, 0x1A, 0x0A];

    private static readonly uint[] Crc32Table = BuildCrc32Table();

    /// <summary>
    /// Возвращает QR-код как документ SVG.
    /// </summary>
    /// <param name="text">Кодируемая строка, обычно платёжная ссылка.</param>
    /// <param name="options">Параметры отрисовки; <see langword="null"/> — значения по умолчанию.</param>
    public static string ToSvg(string text, TBankQrRenderOptions? options = null)
    {
        var settings = Prepare(text, options);
        var code = TBankQrCode.Encode(text, settings.EccLevel);

        var size = code.Size;
        var span = size + (settings.QuietZone * 2);
        var pixels = span * settings.ModuleSize;

        var svg = new StringBuilder(1024 + (size * size / 2));
        svg.Append("<svg xmlns=\"http://www.w3.org/2000/svg\" width=\"")
            .Append(pixels.ToString(CultureInfo.InvariantCulture))
            .Append("\" height=\"")
            .Append(pixels.ToString(CultureInfo.InvariantCulture))
            .Append("\" viewBox=\"0 0 ")
            .Append(span.ToString(CultureInfo.InvariantCulture))
            .Append(' ')
            .Append(span.ToString(CultureInfo.InvariantCulture))
            .Append("\" shape-rendering=\"crispEdges\" role=\"img\">");

        if (settings.LightColor is not null)
        {
            svg.Append("<rect width=\"100%\" height=\"100%\" fill=\"")
                .Append(Escape(settings.LightColor))
                .Append("\"/>");
        }

        // Тёмные модули собираются в один путь: это втрое короче, чем rect на каждый модуль,
        // и заметно быстрее рисуется в браузере на крупных версиях.
        svg.Append("<path fill=\"").Append(Escape(settings.DarkColor)).Append("\" d=\"");
        for (var row = 0; row < size; row++)
        {
            var col = 0;
            while (col < size)
            {
                if (!code.Modules[row, col])
                {
                    col++;
                    continue;
                }

                var run = 1;
                while (col + run < size && code.Modules[row, col + run])
                {
                    run++;
                }

                svg.Append('M')
                    .Append((col + settings.QuietZone).ToString(CultureInfo.InvariantCulture))
                    .Append(' ')
                    .Append((row + settings.QuietZone).ToString(CultureInfo.InvariantCulture))
                    .Append('h')
                    .Append(run.ToString(CultureInfo.InvariantCulture))
                    .Append("v1H")
                    .Append((col + settings.QuietZone).ToString(CultureInfo.InvariantCulture))
                    .Append('z');
                col += run;
            }
        }

        svg.Append("\"/></svg>");
        return svg.ToString();
    }

    /// <summary>
    /// Возвращает QR-код как SVG, закодированный в Base64 — для подстановки в <c>img src</c>.
    /// </summary>
    /// <param name="text">Кодируемая строка, обычно платёжная ссылка.</param>
    /// <param name="options">Параметры отрисовки; <see langword="null"/> — значения по умолчанию.</param>
    public static string ToSvgBase64(string text, TBankQrRenderOptions? options = null) =>
        Convert.ToBase64String(Encoding.UTF8.GetBytes(ToSvg(text, options)));

    /// <summary>
    /// Возвращает QR-код как SVG в виде готового <c>data:</c>-адреса.
    /// </summary>
    /// <param name="text">Кодируемая строка, обычно платёжная ссылка.</param>
    /// <param name="options">Параметры отрисовки; <see langword="null"/> — значения по умолчанию.</param>
    public static string ToSvgDataUri(string text, TBankQrRenderOptions? options = null) =>
        "data:image/svg+xml;base64," + ToSvgBase64(text, options);

    /// <summary>
    /// Возвращает QR-код как изображение PNG.
    /// </summary>
    /// <remarks>
    /// Однобитный оттенок серого: тёмные модули чёрные, фон белый. Цвета из
    /// <see cref="TBankQrRenderOptions"/> к PNG не применяются — для цветного кода нужен SVG.
    /// </remarks>
    /// <param name="text">Кодируемая строка, обычно платёжная ссылка.</param>
    /// <param name="options">Параметры отрисовки; <see langword="null"/> — значения по умолчанию.</param>
    public static byte[] ToPng(string text, TBankQrRenderOptions? options = null)
    {
        var settings = Prepare(text, options);
        var code = TBankQrCode.Encode(text, settings.EccLevel);

        var size = code.Size;
        var span = size + (settings.QuietZone * 2);
        var pixels = span * settings.ModuleSize;
        var stride = (pixels + 7) / 8;

        // Одна строка модулей разворачивается в ModuleSize одинаковых строк пикселей, поэтому
        // упакованный ряд считается один раз и повторяется.
        var raw = new byte[(stride + 1) * pixels];
        var rowBuffer = new byte[stride];
        for (var row = 0; row < span; row++)
        {
            Array.Clear(rowBuffer, 0, stride);
            for (var x = 0; x < pixels; x++)
            {
                var col = (x / settings.ModuleSize) - settings.QuietZone;
                var modelRow = row - settings.QuietZone;
                var dark = modelRow >= 0 && modelRow < size && col >= 0 && col < size
                    && code.Modules[modelRow, col];

                // В однобитном PNG единица — белый, ноль — чёрный.
                if (!dark)
                {
                    rowBuffer[x / 8] |= (byte)(0x80 >> (x % 8));
                }
            }

            for (var repeat = 0; repeat < settings.ModuleSize; repeat++)
            {
                var y = (row * settings.ModuleSize) + repeat;
                var offset = y * (stride + 1);
                raw[offset] = 0; // фильтр строки: без предсказания
                Buffer.BlockCopy(rowBuffer, 0, raw, offset + 1, stride);
            }
        }

        using var png = new MemoryStream();
        png.Write(PngSignature, 0, PngSignature.Length);
        WriteChunk(png, "IHDR", BuildIhdr(pixels));
        WriteChunk(png, "IDAT", Deflate(raw));
        WriteChunk(png, "IEND", []);

        return png.ToArray();
    }

    /// <summary>
    /// Возвращает QR-код как PNG, закодированный в Base64 — для подстановки в <c>img src</c>.
    /// </summary>
    /// <param name="text">Кодируемая строка, обычно платёжная ссылка.</param>
    /// <param name="options">Параметры отрисовки; <see langword="null"/> — значения по умолчанию.</param>
    public static string ToPngBase64(string text, TBankQrRenderOptions? options = null) =>
        Convert.ToBase64String(ToPng(text, options));

    /// <summary>
    /// Возвращает QR-код как PNG в виде готового <c>data:</c>-адреса.
    /// </summary>
    /// <param name="text">Кодируемая строка, обычно платёжная ссылка.</param>
    /// <param name="options">Параметры отрисовки; <see langword="null"/> — значения по умолчанию.</param>
    public static string ToPngDataUri(string text, TBankQrRenderOptions? options = null) =>
        "data:image/png;base64," + ToPngBase64(text, options);

    /// <summary>
    /// Сообщает, откроется ли закодированная ссылка при сканировании штатной камерой телефона.
    /// </summary>
    /// <remarks>
    /// Истина только для <c>http</c> и <c>https</c>. Ссылки с собственной схемой — например
    /// <c>sberpay://</c> из метода SberPay link — приложение «Камера» обычно игнорирует: такой
    /// адрес работает нажатием на том же устройстве, а не через QR. Вызывающий код может
    /// заранее решить, показывать ли покупателю код или только кнопку.
    /// </remarks>
    /// <param name="text">Проверяемая ссылка.</param>
    public static bool IsLikelyScannable(string text) =>
        Uri.TryCreate(text, UriKind.Absolute, out var uri)
        && (uri.Scheme == Uri.UriSchemeHttp || uri.Scheme == Uri.UriSchemeHttps);

    private static TBankQrRenderOptions Prepare(string text, TBankQrRenderOptions? options)
    {
        ArgumentNullException.ThrowIfNull(text, nameof(text));

        if (text.Length == 0)
        {
            throw new ArgumentException("Нечего кодировать: строка пуста.", nameof(text));
        }

        var settings = options ?? new TBankQrRenderOptions();
        settings.Validate();

        return settings;
    }

    private static byte[] BuildIhdr(int pixels)
    {
        var ihdr = new byte[13];
        WriteBigEndian(ihdr, 0, (uint)pixels);
        WriteBigEndian(ihdr, 4, (uint)pixels);
        ihdr[8] = 1; // бит на пиксель
        ihdr[9] = 0; // оттенки серого
        ihdr[10] = 0; // сжатие deflate
        ihdr[11] = 0; // стандартный набор фильтров
        ihdr[12] = 0; // без чередования

        return ihdr;
    }

    /// <summary>
    /// Упаковывает данные в поток zlib: заголовок, сырой deflate и контрольная сумма Adler-32.
    /// </summary>
    private static byte[] Deflate(byte[] raw)
    {
        using var compressed = new MemoryStream();
        compressed.WriteByte(0x78); // метод deflate, окно 32 КиБ
        compressed.WriteByte(0x01); // без предустановленного словаря

        using (var deflate = new DeflateStream(compressed, CompressionLevel.Optimal, leaveOpen: true))
        {
            deflate.Write(raw, 0, raw.Length);
        }

        var adler = Adler32(raw);
        compressed.WriteByte((byte)(adler >> 24));
        compressed.WriteByte((byte)(adler >> 16));
        compressed.WriteByte((byte)(adler >> 8));
        compressed.WriteByte((byte)adler);

        return compressed.ToArray();
    }

    private static void WriteChunk(Stream stream, string type, byte[] data)
    {
        var header = new byte[4];
        WriteBigEndian(header, 0, (uint)data.Length);
        stream.Write(header, 0, 4);

        var typeBytes = Encoding.ASCII.GetBytes(type);
        stream.Write(typeBytes, 0, typeBytes.Length);
        stream.Write(data, 0, data.Length);

        var crc = Crc32(typeBytes, data);
        var trailer = new byte[4];
        WriteBigEndian(trailer, 0, crc);
        stream.Write(trailer, 0, 4);
    }

    private static void WriteBigEndian(byte[] target, int offset, uint value)
    {
        target[offset] = (byte)(value >> 24);
        target[offset + 1] = (byte)(value >> 16);
        target[offset + 2] = (byte)(value >> 8);
        target[offset + 3] = (byte)value;
    }

    private static uint Adler32(byte[] data)
    {
        const uint modulo = 65521;
        uint a = 1;
        uint b = 0;
        foreach (var value in data)
        {
            a = (a + value) % modulo;
            b = (b + a) % modulo;
        }

        return (b << 16) | a;
    }

    private static uint[] BuildCrc32Table()
    {
        var table = new uint[256];
        for (uint i = 0; i < 256; i++)
        {
            var value = i;
            for (var bit = 0; bit < 8; bit++)
            {
                value = (value & 1) != 0 ? 0xEDB88320u ^ (value >> 1) : value >> 1;
            }

            table[i] = value;
        }

        return table;
    }

    private static uint Crc32(byte[] first, byte[] second)
    {
        var crc = 0xFFFFFFFFu;
        foreach (var value in first)
        {
            crc = Crc32Table[(crc ^ value) & 0xFF] ^ (crc >> 8);
        }

        foreach (var value in second)
        {
            crc = Crc32Table[(crc ^ value) & 0xFF] ^ (crc >> 8);
        }

        return crc ^ 0xFFFFFFFFu;
    }

    private static string Escape(string color) =>
        color.Replace("&", "&amp;").Replace("<", "&lt;").Replace(">", "&gt;").Replace("\"", "&quot;");
}
