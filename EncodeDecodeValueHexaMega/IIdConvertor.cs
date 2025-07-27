public interface IIdConvertor
{
    /// <summary>
    /// long -> hexa idabs (16 char)  example:3429598923097226274 -> BC22BD45620C2F98
    /// </summary>
    /// <param name="value"></param>
    string long_hexaid16(long value);
    

    /// <returns></returns>
    /// <summary>
    /// long long,d   /// example: "BC22BD45620C2F98" -> 3429598923097226274
    /// </summary>
    /// <param name="value"></param>
    long FromHexa_Long(string hex);
    /// <returns></returns>
    /// <summary>
    /// __int64,x example: 3429598923097226274 -> 2f98620cbd45bc22
    /// </summary>
    /// <param name="value"></param>
    /// <returns></returns>
    string FromLong_int64(long value);

    /// <returns></returns>
    /// <summary>
    /// cn64 idabs (12 char)
    /// </summary>
    /// <param name="value"></param>
    string FromLong_cn64(ulong value);
}
