using System.Text;

namespace TBankAcquiringNet;

/// <summary>
/// Закодированный QR-код: готовая матрица модулей плюс выбранные версия и маска.
/// </summary>
/// <remarks>
/// Реализация ISO/IEC 18004 в объёме, нужном для платёжной ссылки: режимы «цифры»,
/// «буквы и цифры» и «байты», уровни коррекции L/M/Q/H, версии 1..40. Структурированное
/// присоединение, ECI и Micro QR не поддерживаются — для ссылок они не нужны.
/// <para>
/// Режим выбирается один на всю строку: самый компактный из тех, что покрывают её целиком.
/// Разбиение на сегменты разных режимов дало бы код на версию-две плотнее на смешанных
/// строках, но платёжные ссылки всегда уходят в байтовый режим (в них есть строчные буквы
/// и <c>?</c>, <c>&amp;</c>, <c>=</c>), так что выигрыша бы не было.
/// </para>
/// </remarks>
internal sealed class TBankQrCode
{
    private const string AlphanumericCharset = "0123456789ABCDEFGHIJKLMNOPQRSTUVWXYZ $%*+-./:";

    /// <summary>Порождающий многочлен поля Галуа GF(256), x^8 + x^4 + x^3 + x^2 + 1.</summary>
    private const int GaloisPrimitive = 0x11D;

    private static readonly byte[] GaloisExp = new byte[512];
    private static readonly byte[] GaloisLog = new byte[256];

    static TBankQrCode()
    {
        var x = 1;
        for (var i = 0; i < 255; i++)
        {
            GaloisExp[i] = (byte)x;
            GaloisLog[x] = (byte)i;
            x <<= 1;
            if (x >= 256)
            {
                x ^= GaloisPrimitive;
            }
        }

        for (var i = 255; i < 512; i++)
        {
            GaloisExp[i] = GaloisExp[i - 255];
        }
    }

    private TBankQrCode(int version, int mask, bool[,] modules)
    {
        Version = version;
        Mask = mask;
        Modules = modules;
    }

    /// <summary>Версия символа, 1..40. Определяет размер: <c>version * 4 + 17</c>.</summary>
    internal int Version { get; }

    /// <summary>Номер выбранной маски, 0..7.</summary>
    internal int Mask { get; }

    /// <summary>Размер символа в модулях, без светлой рамки.</summary>
    internal int Size => (Version * 4) + 17;

    /// <summary>Модули символа, индексация <c>[строка, столбец]</c>; <see langword="true"/> — тёмный.</summary>
    internal bool[,] Modules { get; }

    /// <summary>
    /// Кодирует строку в QR-код наименьшей версии, которая её вмещает.
    /// </summary>
    /// <param name="text">Кодируемая строка.</param>
    /// <param name="eccLevel">Уровень коррекции ошибок.</param>
    /// <param name="maskOverride">
    /// Номер маски вместо выбранной по штрафам. Нужен только тестам, чтобы сверять раскладку
    /// данных отдельно от выбора маски.
    /// </param>
    internal static TBankQrCode Encode(string text, TBankQrEccLevel eccLevel, int? maskOverride = null)
    {
        var level = (int)eccLevel;
        var mode = DetectMode(text);
        var payload = mode == Mode.Byte ? Encoding.UTF8.GetBytes(text) : [];
        var dataLength = mode == Mode.Byte ? payload.Length : text.Length;
        var dataBits = CountDataBits(mode, dataLength);

        var version = SelectVersion(mode, dataBits, dataLength, level);
        var dataCodewords = CountDataCodewords(version, level);

        var bits = new BitBuffer(dataCodewords * 8);
        bits.Append((int)mode, 4);
        bits.Append(dataLength, CharCountBits(version, mode));
        AppendPayload(bits, mode, text, payload);

        // Завершитель и выравнивание до целого кодового слова, затем чередующиеся байты-заполнители.
        var capacityBits = dataCodewords * 8;
        bits.Append(0, Math.Min(4, capacityBits - bits.Length));
        bits.Append(0, (8 - (bits.Length % 8)) % 8);
        for (var pad = 0xEC; bits.Length < capacityBits; pad ^= 0xEC ^ 0x11)
        {
            bits.Append(pad, 8);
        }

        var codewords = Interleave(bits.ToBytes(), version, level);
        return Build(version, level, codewords, maskOverride);
    }

    private enum Mode
    {
        Numeric = 1,
        Alphanumeric = 2,
        Byte = 4
    }

    private static Mode DetectMode(string text)
    {
        var numeric = true;
        var alphanumeric = true;
        foreach (var c in text)
        {
            if (c is < '0' or > '9')
            {
                numeric = false;
            }

            if (AlphanumericCharset.IndexOf(c) < 0)
            {
                alphanumeric = false;
                break;
            }
        }

        if (numeric)
        {
            return Mode.Numeric;
        }

        return alphanumeric ? Mode.Alphanumeric : Mode.Byte;
    }

    private static int CharCountBits(int version, Mode mode)
    {
        var range = version <= 9 ? 0 : version <= 26 ? 1 : 2;
        return mode switch
        {
            Mode.Numeric => new[] { 10, 12, 14 }[range],
            Mode.Alphanumeric => new[] { 9, 11, 13 }[range],
            _ => new[] { 8, 16, 16 }[range]
        };
    }

    private static int CountDataBits(Mode mode, int length) => mode switch
    {
        // Три цифры укладываются в 10 бит; остаток в одну или две цифры — в 4 и 7 бит.
        Mode.Numeric => (length / 3 * 10) + ((length % 3) switch { 1 => 4, 2 => 7, _ => 0 }),
        // Два символа — 11 бит, одиночный остаток — 6.
        Mode.Alphanumeric => (length / 2 * 11) + (length % 2 * 6),
        _ => length * 8
    };

    private static void AppendPayload(BitBuffer bits, Mode mode, string text, byte[] payload)
    {
        switch (mode)
        {
            case Mode.Numeric:
                var i = 0;
                for (; i + 3 <= text.Length; i += 3)
                {
                    bits.Append(int.Parse(text.Substring(i, 3)), 10);
                }

                var tail = text.Length - i;
                if (tail > 0)
                {
                    bits.Append(int.Parse(text.Substring(i, tail)), tail == 1 ? 4 : 7);
                }

                break;

            case Mode.Alphanumeric:
                var j = 0;
                for (; j + 2 <= text.Length; j += 2)
                {
                    var pair = (AlphanumericCharset.IndexOf(text[j]) * 45)
                        + AlphanumericCharset.IndexOf(text[j + 1]);
                    bits.Append(pair, 11);
                }

                if (j < text.Length)
                {
                    bits.Append(AlphanumericCharset.IndexOf(text[j]), 6);
                }

                break;

            default:
                foreach (var b in payload)
                {
                    bits.Append(b, 8);
                }

                break;
        }
    }

    private static int SelectVersion(Mode mode, int dataBits, int dataLength, int level)
    {
        for (var version = 1; version <= 40; version++)
        {
            var needed = 4 + CharCountBits(version, mode) + dataBits;
            if (needed <= CountDataCodewords(version, level) * 8)
            {
                return version;
            }
        }

        throw new ArgumentException(
            $"Данные не помещаются в QR-код: {dataLength} символов при уровне коррекции "
            + $"{(TBankQrEccLevel)level} превышают версию 40.",
            nameof(dataLength));
    }

    private static int CountDataCodewords(int version, int level)
    {
        var total = 0;
        foreach (var group in TBankQrTables.EccBlockGroups[version][level])
        {
            total += group[0] * group[1];
        }

        return total;
    }

    /// <summary>
    /// Разбивает данные на блоки, считает для каждого коды коррекции и перемешивает кодовые
    /// слова так, как требует стандарт: сначала все данные по столбцам блоков, затем коррекция.
    /// </summary>
    private static byte[] Interleave(byte[] data, int version, int level)
    {
        var groups = TBankQrTables.EccBlockGroups[version][level];
        var eccPerBlock = TBankQrTables.EccCodewordsPerBlock[version][level];

        var dataBlocks = new List<byte[]>();
        var eccBlocks = new List<byte[]>();
        var offset = 0;
        foreach (var group in groups)
        {
            for (var b = 0; b < group[0]; b++)
            {
                var block = new byte[group[1]];
                Array.Copy(data, offset, block, 0, block.Length);
                offset += block.Length;
                dataBlocks.Add(block);
                eccBlocks.Add(ComputeEcc(block, eccPerBlock));
            }
        }

        var result = new List<byte>(data.Length + (eccBlocks.Count * eccPerBlock));
        var longestBlock = 0;
        foreach (var block in dataBlocks)
        {
            longestBlock = Math.Max(longestBlock, block.Length);
        }

        for (var i = 0; i < longestBlock; i++)
        {
            foreach (var block in dataBlocks)
            {
                if (i < block.Length)
                {
                    result.Add(block[i]);
                }
            }
        }

        for (var i = 0; i < eccPerBlock; i++)
        {
            foreach (var block in eccBlocks)
            {
                result.Add(block[i]);
            }
        }

        return result.ToArray();
    }

    /// <summary>
    /// Коды коррекции Рида — Соломона: деление многочлена данных на порождающий в GF(256).
    /// </summary>
    private static byte[] ComputeEcc(byte[] data, int eccLength)
    {
        var generator = GeneratorPolynomial(eccLength);
        var remainder = new byte[eccLength];
        foreach (var b in data)
        {
            var factor = (byte)(b ^ remainder[0]);
            Array.Copy(remainder, 1, remainder, 0, eccLength - 1);
            remainder[eccLength - 1] = 0;
            for (var i = 0; i < eccLength; i++)
            {
                remainder[i] ^= GaloisMultiply(generator[i], factor);
            }
        }

        return remainder;
    }

    private static byte[] GeneratorPolynomial(int degree)
    {
        var result = new byte[degree];
        result[degree - 1] = 1;
        byte root = 1;
        for (var i = 0; i < degree; i++)
        {
            for (var j = 0; j < degree; j++)
            {
                result[j] = GaloisMultiply(result[j], root);
                if (j + 1 < degree)
                {
                    result[j] ^= result[j + 1];
                }
            }

            root = GaloisMultiply(root, 0x02);
        }

        return result;
    }

    private static byte GaloisMultiply(byte a, byte b) =>
        a == 0 || b == 0 ? (byte)0 : GaloisExp[GaloisLog[a] + GaloisLog[b]];

    private static TBankQrCode Build(int version, int level, byte[] codewords, int? maskOverride)
    {
        var size = (version * 4) + 17;
        var modules = new bool[size, size];
        var isFunction = new bool[size, size];

        DrawFunctionPatterns(modules, isFunction, version, size);
        DrawCodewords(modules, isFunction, codewords, size);

        var bestMask = 0;
        bool[,]? bestModules = null;
        var bestPenalty = int.MaxValue;
        for (var mask = 0; mask < 8; mask++)
        {
            if (maskOverride is not null && mask != maskOverride)
            {
                continue;
            }

            var candidate = (bool[,])modules.Clone();
            ApplyMask(candidate, isFunction, mask, size);

            // Сведения о формате и версии до оценки не ставятся — так требует стандарт
            // (ISO/IEC 18004, 7.8): маска выбирается по символу без служебных полей.
            var penalty = Penalty(candidate, size);
            if (penalty < bestPenalty)
            {
                bestPenalty = penalty;
                bestMask = mask;
                bestModules = candidate;
            }
        }

        var final = bestModules!;
        DrawFormatInfo(final, level, bestMask, size);
        DrawVersionInfo(final, version, size);

        return new TBankQrCode(version, bestMask, final);
    }

    private static void DrawFunctionPatterns(bool[,] modules, bool[,] isFunction, int version, int size)
    {
        // Поисковые узоры по трём углам вместе с разделителями.
        // Порядок здесь значим: узоры пересекаются, и каждый следующий перекрывает предыдущий.
        // Сначала резервируются служебные полосы, затем их перебивают синхронизирующие дорожки
        // в шестой строке и столбце, а поисковые и выравнивающие узоры идут последними.

        // Область сведений о формате: резервируется светлой, заполняется после выбора маски.
        // У левого верхнего угла полоса занимает 9 модулей, у двух других — по 8; лишний
        // зарезервированный модуль сдвинул бы всю последующую раскладку данных.
        for (var i = 0; i < 9; i++)
        {
            Set(modules, isFunction, i, 8, false);
            Set(modules, isFunction, 8, i, false);
        }

        for (var i = 0; i < 8; i++)
        {
            Set(modules, isFunction, size - 1 - i, 8, false);
            Set(modules, isFunction, 8, size - 1 - i, false);
        }

        if (version >= 7)
        {
            for (var i = 0; i < 6; i++)
            {
                for (var k = 9; k <= 11; k++)
                {
                    Set(modules, isFunction, i, size - k, false);
                    Set(modules, isFunction, size - k, i, false);
                }
            }
        }

        // Синхронизирующие дорожки по шестой строке и шестому столбцу. Их крайние модули
        // попадают на полосу сведений о формате и должны остаться тёмными: сами сведения
        // шестую позицию пропускают, см. DrawFormatInfo.
        for (var i = 8; i < size - 8; i++)
        {
            var dark = i % 2 == 0;
            Set(modules, isFunction, 6, i, dark);
            Set(modules, isFunction, i, 6, dark);
        }

        DrawFinder(modules, isFunction, 0, 0, size);
        DrawFinder(modules, isFunction, 0, size - 7, size);
        DrawFinder(modules, isFunction, size - 7, 0, size);

        DrawAlignmentPatterns(modules, isFunction, version);
    }

    private static void DrawFinder(bool[,] modules, bool[,] isFunction, int row, int col, int size)
    {
        // Сам узор 7×7 плюс светлый разделитель по его границе — отсюда диапазон -1..7.
        for (var dr = -1; dr <= 7; dr++)
        {
            for (var dc = -1; dc <= 7; dc++)
            {
                var r = row + dr;
                var c = col + dc;
                if (r < 0 || r >= size || c < 0 || c >= size)
                {
                    continue;
                }

                var distance = Math.Max(Math.Abs(dr - 3), Math.Abs(dc - 3));
                Set(modules, isFunction, r, c, distance != 2 && distance <= 3);
            }
        }
    }

    private static void DrawAlignmentPatterns(bool[,] modules, bool[,] isFunction, int version)
    {
        var positions = TBankQrTables.AlignmentPositions[version];
        if (positions.Length == 0)
        {
            return;
        }

        var first = positions[0];
        var last = positions[positions.Length - 1];
        foreach (var row in positions)
        {
            foreach (var col in positions)
            {
                // Три позиции заняты поисковыми узорами.
                var onFinder = (row == first && col == first)
                    || (row == first && col == last)
                    || (row == last && col == first);
                if (onFinder)
                {
                    continue;
                }

                for (var dr = -2; dr <= 2; dr++)
                {
                    for (var dc = -2; dc <= 2; dc++)
                    {
                        var distance = Math.Max(Math.Abs(dr), Math.Abs(dc));
                        Set(modules, isFunction, row + dr, col + dc, distance != 1);
                    }
                }
            }
        }
    }

    private static void Set(bool[,] modules, bool[,] isFunction, int row, int col, bool dark)
    {
        modules[row, col] = dark;
        isFunction[row, col] = true;
    }

    /// <summary>
    /// Раскладывает поток кодовых слов по символу: парами столбцов справа налево, попеременно
    /// вверх и вниз, пропуская синхронизирующий столбец.
    /// </summary>
    private static void DrawCodewords(bool[,] modules, bool[,] isFunction, byte[] codewords, int size)
    {
        var bit = 0;
        var totalBits = codewords.Length * 8;
        for (var right = size - 1; right >= 1; right -= 2)
        {
            var column = right <= 6 ? right - 1 : right;
            for (var vertical = 0; vertical < size; vertical++)
            {
                for (var z = 0; z < 2; z++)
                {
                    var col = column - z;
                    var upwards = ((column & 2) == 0) ^ (col < 6);
                    var row = upwards ? size - 1 - vertical : vertical;
                    if (isFunction[row, col] || bit >= totalBits)
                    {
                        continue;
                    }

                    modules[row, col] = ((codewords[bit >> 3] >> (7 - (bit & 7))) & 1) != 0;
                    bit++;
                }
            }
        }
    }

    private static void ApplyMask(bool[,] modules, bool[,] isFunction, int mask, int size)
    {
        for (var row = 0; row < size; row++)
        {
            for (var col = 0; col < size; col++)
            {
                if (isFunction[row, col])
                {
                    continue;
                }

                var invert = mask switch
                {
                    0 => (row + col) % 2 == 0,
                    1 => row % 2 == 0,
                    2 => col % 3 == 0,
                    3 => (row + col) % 3 == 0,
                    4 => ((row / 2) + (col / 3)) % 2 == 0,
                    5 => (row * col % 2) + (row * col % 3) == 0,
                    6 => ((row * col % 2) + (row * col % 3)) % 2 == 0,
                    _ => ((row * col % 3) + ((row + col) % 2)) % 2 == 0
                };

                modules[row, col] ^= invert;
            }
        }
    }

    private static void DrawFormatInfo(bool[,] modules, int level, int mask, int size)
    {
        var format = TBankQrTables.FormatInfo[(level * 8) + mask];
        var vertical = 0;
        var horizontal = 0;
        for (var i = 0; i < 8; i++)
        {
            var low = ((format >> i) & 1) != 0;
            var high = ((format >> (14 - i)) & 1) != 0;
            if (i == 6)
            {
                // Шестая строка и шестой столбец заняты синхронизирующей дорожкой.
                vertical++;
                horizontal = 1;
            }

            modules[i + vertical, 8] = low;
            modules[8, i + horizontal] = high;
            modules[8, size - 1 - i] = low;
            modules[size - 1 - i, 8] = high;
        }

        // Постоянно тёмный модуль под левым нижним поисковым узором.
        modules[size - 8, 8] = true;
    }

    private static void DrawVersionInfo(bool[,] modules, int version, int size)
    {
        if (version < 7)
        {
            return;
        }

        var info = TBankQrTables.VersionInfo[version - 7];
        for (var i = 0; i < 6; i++)
        {
            for (var k = 0; k < 3; k++)
            {
                var dark = ((info >> ((i * 3) + k)) & 1) != 0;
                modules[size - 11 + k, i] = dark;
                modules[i, size - 11 + k] = dark;
            }
        }
    }

    /// <summary>
    /// Штраф за узнаваемость, по таблице 11 стандарта: длинные одноцветные серии, одноцветные
    /// квадраты 2×2, ложные поисковые узоры и перекос в долю тёмных модулей.
    /// </summary>
    private static int Penalty(bool[,] modules, int size)
    {
        const int n1 = 3;
        const int n2 = 3;
        const int n3 = 40;
        const int n4 = 10;

        var score = 0;
        var dark = 0;
        var row = new bool[size];
        var column = new bool[size];

        for (var i = 0; i < size; i++)
        {
            var rowRun = 0;
            var colRun = 0;
            var rowPrev = false;
            var colPrev = false;
            for (var j = 0; j < size; j++)
            {
                row[j] = modules[i, j];
                column[j] = modules[j, i];
                if (modules[i, j])
                {
                    dark++;
                }

                if (j > 0 && row[j] == rowPrev)
                {
                    rowRun++;
                }
                else
                {
                    if (rowRun >= 5)
                    {
                        score += n1 + (rowRun - 5);
                    }

                    rowRun = 1;
                }

                if (j > 0 && column[j] == colPrev)
                {
                    colRun++;
                }
                else
                {
                    if (colRun >= 5)
                    {
                        score += n1 + (colRun - 5);
                    }

                    colRun = 1;
                }

                // Одноцветный квадрат 2×2 с верхним левым углом в (i-1, j-1).
                if (i > 0 && j > 0
                    && modules[i, j] == rowPrev
                    && modules[i, j] == modules[i - 1, j]
                    && modules[i, j] == modules[i - 1, j - 1])
                {
                    score += n2;
                }

                rowPrev = row[j];
                colPrev = column[j];
            }

            if (rowRun >= 5)
            {
                score += n1 + (rowRun - 5);
            }

            if (colRun >= 5)
            {
                score += n1 + (colRun - 5);
            }

            score += n3 * FinderLikePatterns(row);
            score += n3 * FinderLikePatterns(column);
        }

        var percent = dark * 100.0 / (size * size);
        score += n4 * (int)(Math.Abs(percent - 50) / 5);

        return score;
    }

    /// <summary>
    /// Считает в строке узоры 1:1:3:1:1, окаймлённые светлой зоной в четыре модуля хотя бы с
    /// одной стороны — именно их сканер может принять за поисковый узор.
    /// </summary>
    private static int FinderLikePatterns(bool[] line)
    {
        var count = 0;
        for (var i = 0; i + 7 <= line.Length; i++)
        {
            if (!(line[i] && !line[i + 1] && line[i + 2] && line[i + 3]
                && line[i + 4] && !line[i + 5] && line[i + 6]))
            {
                continue;
            }

            if (IsLightRun(line, i - 4, 4) || IsLightRun(line, i + 7, 4))
            {
                count++;
                i += 3;
            }
        }

        return count;
    }

    private static bool IsLightRun(bool[] line, int start, int length)
    {
        var from = Math.Max(start, 0);
        var to = Math.Min(start + length, line.Length);
        if (from >= to)
        {
            // Узор прижат к краю символа: за краем светлая рамка, условие выполнено.
            return true;
        }

        for (var i = from; i < to; i++)
        {
            if (line[i])
            {
                return false;
            }
        }

        return true;
    }

    /// <summary>
    /// Накопитель битов: собирает поток от индикатора режима до байтов-заполнителей.
    /// </summary>
    private sealed class BitBuffer(int capacityBytes)
    {
        private readonly List<byte> _bytes = new(capacityBytes);
        private int _partial;
        private int _partialBits;

        internal int Length => (_bytes.Count * 8) + _partialBits;

        internal void Append(int value, int bitCount)
        {
            for (var i = bitCount - 1; i >= 0; i--)
            {
                _partial = (_partial << 1) | ((value >> i) & 1);
                _partialBits++;
                if (_partialBits != 8)
                {
                    continue;
                }

                _bytes.Add((byte)_partial);
                _partial = 0;
                _partialBits = 0;
            }
        }

        internal byte[] ToBytes()
        {
            if (_partialBits > 0)
            {
                throw new InvalidOperationException(
                    "Поток битов не выровнен по байту — заполнение выполнено не полностью.");
            }

            return _bytes.ToArray();
        }
    }
}
