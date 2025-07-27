


using System.Text;


public class IdConvertor : IIdConvertor
{
    const string ALPHABET = "0123456789ABCDEFGHIJKLMNOPQRSTUVWXYZabcdefghijklmnopqrstuvwxyz()";



    /// <summary>
    /// +
    /// </summary>
    /// <param name="value"></param>
    /// <returns></returns>
    public string long_hexaid16(long value)
    {
        byte[] bytes = BitConverter.GetBytes(value);
        for (int i = 0; i < 8; i += 2)
        {
            byte temp = bytes[i];
            bytes[i] = bytes[i + 1];
            bytes[i + 1] = temp;
        }

        StringBuilder result = new StringBuilder(16);
        for (int i = 0; i < 8; i++)
        {
            result.Append(bytes[i].ToString("X2"));
        }
        return result.ToString();
    }
    /// <summary>
    /// __int64,x example: 3429598923097226274 -> 2f98620cbd45bc22
    /// </summary>
    /// <param name="value"></param>
    /// <returns></returns>
    public string FromLong_int64(long value)
    {
        uint uLong1 = (uint)(value & 0xFFFFFFFF);
        uint uLong2 = (uint)(value >> 32);
        return $"{uLong2:X8}{uLong1:X8}".ToString();
    }
    /// <summary>
    /// example: "BC22BD45620C2F98" -> 3429598923097226274
    /// </summary>
    /// <param name="hex"></param>
    /// <returns></returns>
    /// <exception cref="ArgumentException"></exception>
    public long FromHexa_Long(string hex)
    {
        if (hex.Length != 16)
            throw new ArgumentException("Hex string must be exactly 16 characters long.");

        byte[] bytes = new byte[8];

        // Преобразуем строку в байты
        for (int i = 0; i < 8; i++)
        {
            bytes[i] = Convert.ToByte(hex.Substring(i * 2, 2), 16);
        }

        // Обратная перестановка байтов (та же логика, что и в ToHexaIdabs, но в обратную сторону — на самом деле, та же самая)
        for (int i = 0; i < 8; i += 2)
        {
            byte temp = bytes[i];
            bytes[i] = bytes[i + 1];
            bytes[i + 1] = temp;
        }

        // Преобразуем обратно в long
        return BitConverter.ToInt64(bytes, 0);
    }

    /// <summary>
    /// Long ->cn64, 3429598923097226274 > L82l5rB3YXvB
    /// </summary>
    /// <param name="value"></param>
    /// <returns></returns>
    public string FromLong_cn64(ulong value)
    {
        
        // Преобразуем ulong в массив байтов (8 байт)
        byte[] valueBytes = BitConverter.GetBytes(value);

        // Приводим к big-endian
        if (BitConverter.IsLittleEndian)
            Array.Reverse(valueBytes);

        // Создаём буфер из 9 байт: 8 байт значения + 1 байт контрольной суммы
        byte[] buffer = new byte[9];
        byte checksum = 0;

        for (int i = 0; i < 8; i++)
        {
            buffer[i] = valueBytes[i];
            checksum += valueBytes[i];
        }

        buffer[8] = checksum;

        // Кодируем 9 байт в 12 символов (каждые 3 байта → 4 символа)
        StringBuilder result = new StringBuilder(13); // 12 символов + '\0'

        for (int i = 0; i < 9; i += 3)
        {
            int acc = (buffer[i] << 16) | (buffer[i + 1] << 8) | buffer[i + 2];

            for (int j = 0; j < 4; j++)
            {
                int sixBits = acc & 0x3F;
                acc >>= 6;

                char symbol;
                if (sixBits < 10)
                    symbol = (char)('0' + sixBits);
                else if (sixBits < 36)
                    symbol = (char)('A' + (sixBits - 10));
                else if (sixBits < 62)
                    symbol = (char)('a' + (sixBits - 36));
                else
                    symbol = (char)('(' + (sixBits - 62)); // 62 → '(', 63 → ')'

                result.Append(symbol);
            }
        }

        return FixGcn64Order(result.ToString());
    }
    string FixGcn64Order(ReadOnlySpan<char> input)
    {
        if (input.Length != 12)
            throw new ArgumentException("Строка должна содержать ровно 12 символов.");

        Span<char> result = stackalloc char[12];

        // Копируем блоки в обратном порядке: B3 (8-11), B2 (4-7), B1 (0-3)
        input.Slice(8, 4).CopyTo(result.Slice(0, 4));
        input.Slice(4, 4).CopyTo(result.Slice(4, 4));
        input.Slice(0, 4).CopyTo(result.Slice(8, 4));

        return new string(result);
    }
}