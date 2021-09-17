using System;
using System.Collections.Generic;
using System.Linq;

public static class Helper
{
    private static Random random = new Random();
    /// <summary>
    /// 单体 一列
    /// </summary>
    /// <typeparam name="T"></typeparam>
    /// <param name="t"></param>
    /// <returns></returns>
    public static T[] Singular<T>(T t) => new[] {t};

    public static T RandomPick<T>(this IEnumerable<T> data) where T : class
    {
        var list = data.ToList();
        if (!list.Any()) return null;
        var pick = random.Next(0, list.Count);
        return list[pick];
    }
}