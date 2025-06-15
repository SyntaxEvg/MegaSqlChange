using System;
using System.Collections.Generic;
using System.Text;
using System.Text.RegularExpressions;

public static class EncodingAnalyzer
{
    private static readonly Regex CyrillicPattern = new Regex(@"[\u0410-\u044F]");
    private static readonly Regex BrokenUtf8Pattern = new Regex(@"Ð[°-яА-Я]|Ñ[°-яА-Я]");

    public static bool NeedsFix(string text)
    {
        // Проверяем наличие битой UTF-8 кодировки
        if (BrokenUtf8Pattern.IsMatch(text))
            return true;

        // Проверяем отсутствие кириллицы там, где она должна быть
        if (!CyrillicPattern.IsMatch(text) && ContainsExpectedCyrillic(text))
            return true;

        return false;
    }

    private static bool ContainsExpectedCyrillic(string text)
    {
        // Проверяем наличие паттернов, которые обычно указывают на русский текст
        var expectedPatterns = new[]
        {
            @"ов\b", // окончания фамилий
            @"ич\b",
            @"ский\b",
            @"Ð\w+", // типичные паттерны битой кодировки
            @"Ñ\w+"
        };

        return expectedPatterns.Any(pattern => Regex.IsMatch(text, pattern));
    }
}
public static class EncodingDetector
{
    // Паттерны для определения "сломанной" кодировки
    private static readonly string[] BrokenPatterns = new[]
    {
        @"Ð\w", // Находит символы начинающиеся с Ð
        @"Ñ\w", // Находит символы начинающиеся с Ñ
        @"[\xC0-\xDF][\x80-\xBF]", // UTF-8 двухбайтовые последовательности
        @"[\xE0-\xEF][\x80-\xBF]{2}", // UTF-8 трехбайтовые последовательности
       // @"â€™|â€"|â€œ|â€\w", // Типичные признаки неправильной кодировки
        @"Ð¸|Ð¹|Ðº|Ð»|Ð¼|Ð½|Ð¾|Ð¿" // Часто встречающиеся последовательности
    };

    public static bool IsBrokenEncoding(string text)
    {
        foreach (var pattern in BrokenPatterns)
        {
            if (Regex.IsMatch(text, pattern))
                return true;
        }
        return false;
    }

    public static string FixEncoding(string text)
    {
        if (!IsBrokenEncoding(text))
            return text;

        try
        {
            // Пробуем исправить кодировку
            byte[] bytes = Encoding.GetEncoding("ISO-8859-1").GetBytes(text);
            return Encoding.UTF8.GetString(bytes);
        }
        catch
        {
            return text;
        }
    }

    // Более сложный метод с проверкой различных паттернов
    public static string FixEncodingWithPatterns(string text)
    {
        // Паттерны замены
        var replacements = new Dictionary<string, string>
        {
            {@"Ð°", "а"},
            {@"Ð±", "б"},
            {@"Ð²", "в"},
            // Добавьте другие соответствия по необходимости
        };

        foreach (var replacement in replacements)
        {
            text = Regex.Replace(text, replacement.Key, replacement.Value);
        }

        return text;
    }

    // Метод для определения типа неправильной кодировки
    public static string DetectEncodingType(string text)
    {
        if (Regex.IsMatch(text, @"Ð\w"))
            return "Возможно UTF-8 как Latin-1";

        if (Regex.IsMatch(text, @"Ã¢â‚¬â„¢"))
            return "Множественное неправильное декодирование";

        return "Неизвестный тип кодировки";
    }
}

//ÀÂÃÆÇÉÈÊËÎÏÔŒÙÛÜŸàâãæçéèêëîïôœùûüÿÄÖÜẞßäöüÁÉÍÑÓÔÕÚÜáéíñóôõúüĄĆĘŁŃÓŚŹŻąćęłńóśźżÇĞİIÖŞÜçğıiöşü

//public class ConsoleLogger : ILogger
//{
//    public void Log(string message)
//    {
//        Console.WriteLine(message);
//    }
//}

//class Program
//{
//    static void Main()
//    {
//        var converter = new LatinaToCyr(new ConsoleLogger());

//        string input = "Пример текста с символами &#x410; и u0410 и <b>HTML</b>";
//        string result = converter.ClearSymbol(input);

//        Console.WriteLine("Результат: " + result);
//    }
//}


public interface ILogger
{
    void Log(string message);
}

public class LatinaToCyr
{
    private Dictionary<string, char> arrayTranslateutf16 = new Dictionary<string, char>();
    private ILogger logger;

    public LatinaToCyr(ILogger logger = null)
    {
        this.logger = logger;
        InitArray();
    }

    private void InitArray()
    {
        arrayTranslateutf16[CharPair(1056, 1106)] = (char)1040;
        arrayTranslateutf16[CharPair(1056, 8216)] = (char)1041;
        arrayTranslateutf16[CharPair(1056, 8217)] = (char)1042;
        arrayTranslateutf16[CharPair(1056, 8220)] = (char)1043;
        arrayTranslateutf16[CharPair(1056, 8221)] = (char)1044;
        arrayTranslateutf16[CharPair(1056, 8226)] = (char)1045;
        arrayTranslateutf16[CharPair(1056, 8211)] = (char)1046;
        arrayTranslateutf16[CharPair(1056, 8212)] = (char)1047;
        arrayTranslateutf16[CharPair(1056, 152)] = (char)1048;
        arrayTranslateutf16[CharPair(1056, 8482)] = (char)1049;
        arrayTranslateutf16[CharPair(1056, 1113)] = (char)1050;
        arrayTranslateutf16[CharPair(1056, 8250)] = (char)1051;
        arrayTranslateutf16[CharPair(1056, 1114)] = (char)1052;
        arrayTranslateutf16[CharPair(1056, 1116)] = (char)1053;
        arrayTranslateutf16[CharPair(1056, 1115)] = (char)1054;
        arrayTranslateutf16[CharPair(1056, 1119)] = (char)1055;
        arrayTranslateutf16[CharPair(1056, 160)] = (char)1056;
        arrayTranslateutf16[CharPair(1056, 1038)] = (char)1057;
        arrayTranslateutf16[CharPair(1056, 1118)] = (char)1058;
        arrayTranslateutf16[CharPair(1057, 1118)] = (char)1058;
        arrayTranslateutf16[CharPair(1056, 1032)] = (char)1059;
        arrayTranslateutf16[CharPair(1056, 164)] = (char)1060;
        arrayTranslateutf16[CharPair(1056, 1168)] = (char)1061;
        arrayTranslateutf16[CharPair(1056, 166)] = (char)1062;
        arrayTranslateutf16[CharPair(1056, 167)] = (char)1063;
        arrayTranslateutf16[CharPair(1056, 1025)] = (char)1064;
        arrayTranslateutf16[CharPair(1056, 169)] = (char)1065;
        arrayTranslateutf16[CharPair(1056, 1028)] = (char)1066;
        arrayTranslateutf16[CharPair(1056, 171)] = (char)1067;
        arrayTranslateutf16[CharPair(1056, 172)] = (char)1068;
        arrayTranslateutf16[CharPair(1056, 173)] = (char)1069;
        arrayTranslateutf16[CharPair(1056, 174)] = (char)1070;
        arrayTranslateutf16[CharPair(1056, 1031)] = (char)1071;
        arrayTranslateutf16[CharPair(1056, 176)] = (char)1072;
        arrayTranslateutf16[CharPair(1056, 177)] = (char)1073;
        arrayTranslateutf16[CharPair(1056, 1030)] = (char)1074;
        arrayTranslateutf16[CharPair(1056, 1110)] = (char)1075;
        arrayTranslateutf16[CharPair(1056, 1169)] = (char)1076;
        arrayTranslateutf16[CharPair(1056, 181)] = (char)1077;
        arrayTranslateutf16[CharPair(1056, 182)] = (char)1078;
        arrayTranslateutf16[CharPair(1056, 183)] = (char)1079;
        arrayTranslateutf16[CharPair(1056, 1105)] = (char)1080;
        arrayTranslateutf16[CharPair(1056, 8470)] = (char)1081;
        arrayTranslateutf16[CharPair(1056, 1108)] = (char)1082;
        arrayTranslateutf16[CharPair(1056, 187)] = (char)1083;
        arrayTranslateutf16[CharPair(1056, 1112)] = (char)1084;
        arrayTranslateutf16[CharPair(1056, 1029)] = (char)1085;
        arrayTranslateutf16[CharPair(1056, 1109)] = (char)1086;
        arrayTranslateutf16[CharPair(1056, 1111)] = (char)1087;
        arrayTranslateutf16[CharPair(1057, 1111)] = (char)1087;
        arrayTranslateutf16[CharPair(1057, 1026)] = (char)1088;
        arrayTranslateutf16[CharPair(1056, 1026)] = (char)1088;
        arrayTranslateutf16[CharPair(1057, 1027)] = (char)1089;
        arrayTranslateutf16[CharPair(1057, 8218)] = (char)1090;
        arrayTranslateutf16[CharPair(1057, 1107)] = (char)1091;
        arrayTranslateutf16[CharPair(1057, 8222)] = (char)1092;
        arrayTranslateutf16[CharPair(1057, 8230)] = (char)1093;
        arrayTranslateutf16[CharPair(1057, 8224)] = (char)1094;
        arrayTranslateutf16[CharPair(1057, 8225)] = (char)1095;
        arrayTranslateutf16[CharPair(1057, 8364)] = (char)1096;
        arrayTranslateutf16[CharPair(1057, 8240)] = (char)1097;
        arrayTranslateutf16[CharPair(1057, 1033)] = (char)1098;
        arrayTranslateutf16[CharPair(1057, 8249)] = (char)1099;
        arrayTranslateutf16[CharPair(1057, 1034)] = (char)1100;
        arrayTranslateutf16[CharPair(1057, 1036)] = (char)1101;
        arrayTranslateutf16[CharPair(1057, 1035)] = (char)1102;
        arrayTranslateutf16[CharPair(1057, 1039)] = (char)1103;
        arrayTranslateutf16[CharPair(1056, 1027)] = (char)1025;
        arrayTranslateutf16[CharPair(1057, 8216)] = (char)1105;
    }

    private string CharPair(int a, int b)
    {
        return $"{(char)a}{(char)b}";
    }

    public string ClearSymbol(string str)
    {
        return ClearSymbolAndUpperCase(str, false);
    }

    public string ClearSymbolAndUpperCase(string str, bool flag)
    {
        if (string.IsNullOrEmpty(str)) return "";

        string result = LatToCyrNewSubj(str, flag);

        result = Regex.Replace(result, @"u([0-9A-Fa-f]{4})", m =>
        {
            return ((char)Convert.ToInt32(m.Groups[1].Value, 16)).ToString();
        });

        result = Regex.Replace(result, @"&#([0-9]+);", m =>
        {
            return ((char)Convert.ToInt32(m.Groups[1].Value)).ToString();
        });

        result = Regex.Replace(result, @"&#x([0-9a-fA-F]+);", m =>
        {
            return ((char)Convert.ToInt32(m.Groups[1].Value, 16)).ToString();
        });

        result = Regex.Replace(result, @"<[^>]+>", " ");
        result = result.Replace("''", "'").Replace("\\", "&quot;");

        result = UTF16toWindows1251(result);

        return result;
    }

    public string LatToCyrNewSubj(string inStr, bool toUpperCase)
    {
        if (string.IsNullOrEmpty(inStr)) return inStr;

        StringBuilder outStr = new StringBuilder();

        foreach (char c in inStr)
        {
            int code = (int)c;
            string res = "";

            switch (true)
            {
                case bool _ when (code >= 192 && code < 256):
                    res = ((char)(code + 848)).ToString();
                    break;
                case bool _ when (code == 168):
                    res = ((char)1025).ToString();
                    break;
                case bool _ when (code == 184):
                    res = ((char)1105).ToString();
                    break;
                case bool _ when (code == 376):
                    res = ((char)1103).ToString();
                    break;
                case bool _ when (code == 34):
                    res = ((char)96).ToString();
                    break;
                case bool _ when (code == 13 || code == 10 || code == 9 || code == 11):
                    res = " ";
                    break;
                case bool _ when (code >= 1040 && code < 1106):
                    res = ((char)code).ToString();
                    break;
                case bool _ when (code == 92 || code == 185 || code == 0):
                    res = "";
                    break;
                default:
                    res = ((char)code).ToString();
                    break;
            }

            outStr.Append(res);
        }

        return toUpperCase ? outStr.ToString().ToUpper() : outStr.ToString();
    }

    public string UTF16toWindows1251(string str)
    {
        if (string.IsNullOrEmpty(str)) return str;

        StringBuilder outStr = new StringBuilder();

        int i = 0;
        while (i < str.Length)
        {
            if (i + 1 < str.Length)
            {
                string pair = str.Substring(i, 2);
                if (arrayTranslateutf16.TryGetValue(pair, out char translated))
                {
                    outStr.Append(translated);
                    i += 2; // обработали пару
                    continue;
                }
            }

            // либо последний символ, либо пара не найдена
            outStr.Append(str[i]);
            i++;
        }


        //for (int i = 0; i < str.Length - 1; i++)
        //{
        //    string pair = str.Substring(i, 2);
        //    if (arrayTranslateutf16.TryGetValue(pair, out char translated))
        //    {
        //        outStr.Append(translated);
        //        i++; // skip next char
        //    }
        //    else
        //    {
        //        outStr.Append(str[i]);
        //    }
        //}

        // Append last char if not processed
        if (str.Length % 2 != 0)
        {
            outStr.Append(str[^1]);
        }

        return outStr.ToString();
    }

    public string CharCodeAt(string str)
    {
        if (string.IsNullOrEmpty(str)) return "";

        StringBuilder sb = new StringBuilder();
        foreach (char c in str)
        {
            sb.Append(((int)c).ToString()).Append(";");
        }

        return sb.ToString();
    }

    private void Log(string message)
    {
        logger?.Log(message);
    }
}