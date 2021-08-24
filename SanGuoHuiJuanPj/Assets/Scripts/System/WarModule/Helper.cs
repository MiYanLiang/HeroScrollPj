public sealed class Helper
{
    /// <summary>
    /// 单体 一列
    /// </summary>
    /// <typeparam name="T"></typeparam>
    /// <param name="t"></param>
    /// <returns></returns>
    public static T[] Singular<T>(T t) => new[] {t};
}