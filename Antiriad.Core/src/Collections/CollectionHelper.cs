using System.Collections;
using Antiriad.Core.Helpers;

namespace Antiriad.Core.Collections;

/// <summary>
/// Static class for Collection tools
/// </summary>
public static class CollectionHelper
{
    /// <summary>
    /// Gets the value associated with the specified key ignoring case for string keys
    /// </summary>
    /// <typeparam name="TKey"></typeparam>
    /// <typeparam name="TValue"></typeparam>
    /// <param name="dict"></param>
    /// <param name="key"></param>
    /// <param name="value"></param>
    /// <returns></returns>
    public static bool TryGetValueIgnoreCase<TKey, TValue>(this IDictionary<TKey, TValue> dict, TKey key, out TValue? value)
    {
        if (key is not string k || !dict.Any())
        {
            value = default;
            return false;
        }

        return dict is not IDictionary<string, TValue> d ? dict.TryGetValue(key, out value) : (value = d.Find(i => i.Key.EqualsOrdinalIgnoreCase(k)).Value) != null;
    }

    /// <summary>
    /// Gets the value associated with the specified key and convert it
    /// </summary>
    /// <typeparam name="TKey"></typeparam>
    /// <typeparam name="TValue"></typeparam>
    /// <typeparam name="TOut"></typeparam>
    /// <param name="dict"></param>
    /// <param name="key"></param>
    /// <param name="defvalue"></param>
    /// <returns></returns>
    public static TOut? Get<TKey, TValue, TOut>(this IDictionary<TKey, TValue> dict, TKey key, TOut? defvalue)
    {
        return (dict.TryGetValue(key, out var item) || dict.TryGetValueIgnoreCase(key, out item)) ? Typer.To<TOut>(item) : defvalue;
    }

    /// <summary>
    /// Gets the value associated with the specified key or empty if not found
    /// </summary>
    /// <typeparam name="TKey"></typeparam>
    /// <typeparam name="TValue"></typeparam>
    /// <param name="dict"></param>
    /// <param name="key"></param>
    /// <returns></returns>
    public static TValue? Get<TKey, TValue>(this IDictionary<TKey, TValue> dict, TKey key)
    {
        return (dict.TryGetValue(key, out var item) || dict.TryGetValueIgnoreCase(key, out item)) ? Typer.To<TValue>(item) : default;
    }

    /// <summary>
    /// 
    /// </summary>
    /// <typeparam name="T"></typeparam>
    /// <param name="source"></param>
    /// <param name="action"></param>
    public static void ForEach<T>(this IEnumerable<T> source, Action<T> action)
    {
        foreach (var i in source)
            action(i);
    }

    /// <summary>
    /// 
    /// </summary>
    /// <param name="source"></param>
    /// <param name="action"></param>
    /// <typeparam name="T"></typeparam>
    public static void ForEach<T>(this IEnumerable<T> source, Action<int, T> action)
    {
        var x = 0;
        foreach (var i in source)
            action(x++, i);
    }

    /// <summary>
    /// 
    /// </summary>
    /// <typeparam name="T"></typeparam>
    /// <param name="source"></param>
    /// <param name="action"></param>
    public static void ForEach<T>(this IList<T> source, Action<T> action)
    {
        var count = source.Count;
        for (var i = 0; i < count; i++)
            action(source[i]);
    }

    /// <summary>
    /// 
    /// </summary>
    /// <typeparam name="T"></typeparam>
    /// <param name="source"></param>
    /// <param name="action"></param>
    /// <param name="item"></param>
    /// <returns></returns>
    public static bool Find<T>(this IEnumerable source, Predicate<T> action, out T? item)
    {
        foreach (T i in source)
            if (action(i))
            {
                item = i;
                return true;
            }

        item = default;
        return false;
    }

    /// <summary>
    /// 
    /// </summary>
    /// <typeparam name="T"></typeparam>
    /// <param name="source"></param>
    /// <param name="action"></param>
    /// <returns></returns>
    public static T? Find<T>(this IEnumerable<T> source, Predicate<T> action)
    {
        Find(source, action, out var item);
        return item;
    }

    public static T? Find<T>(this IReadOnlyList<T> source, Predicate<T> action)
    {
        Find(source, action, out var item);
        return item;
    }

    public static IEnumerable<E> Distinct<E, R>(this IEnumerable<E> source, Func<E, R> compareValue) where R : notnull
    {
        Dictionary<R, E> elems = new();

        foreach (var i in source)
        {
            if (elems.TryAdd(compareValue(i), i))
                yield return i;
        }

        yield break;
    }

    public static void ForEach<T>(this IEnumerable<T> source, Action<T> action, Action<Exception> catchAction)
    {
        foreach (var item in source)
        {
            try
            {
                action(item);
            }
            catch (Exception ex)
            {
                catchAction(ex);
            }
        }
    }

    /// <summary>
    /// Gets a range of elements from a IList
    /// </summary>
    /// <typeparam name="T"></typeparam>
    /// <param name="source"></param>
    /// <param name="index"></param>
    /// <param name="length"></param>
    /// <returns></returns>
    public static IList<T> GetSlice<T>(this IList<T> source, int index, int length = -1)
    {
        if (length == -1)
            length = Math.Max(0, source.Count - index);

        if (index == 0 && length == source.Count)
            return source;

        var value = new T[length];

        if (source is Array arr)
        {
            Array.Copy(arr, index, value, 0, length);
        }
        else
        {
            for (var i = 0; i < length; i++)
                value[i] = source[index + i];
        }

        return value;
    }

    /// <summary>
    /// Adds an item to a collection and returns the item
    /// </summary>
    /// <typeparam name="T"></typeparam>
    /// <param name="list"></param>
    /// <param name="item"></param>
    /// <returns></returns>
    public static T AddItem<T>(this ICollection<T> list, T item)
    {
        list.Add(item);
        return item;
    }

    /// <summary>
    /// Returns true if an string enumerable contains a value ignoring case
    /// </summary>
    /// <param name="list"></param>
    /// <param name="value"></param>
    /// <returns></returns>
    public static bool ContainsIgnoreCase(this IEnumerable<string> list, string value)
    {
        return list.Any(i => i.EqualsOrdinalIgnoreCase(value));
    }

    private static IList EnumToList(IEnumerable i)
    {
        var list = new ArrayList();
        foreach (var item in i)
            list.Add(item);
        return list;
    }

    public static ICollection ToCollection(this IEnumerable i)
    {
        return i as ICollection ?? EnumToList(i);
    }

    /// <summary>
    /// 
    /// </summary>
    /// <param name="value"></param>
    /// <returns></returns>
    public static int Count(this IEnumerable? value)
    {
        if (value == null)
            return 0;
        if (value is Array array)
            return array.Length;
        if (value is ICollection collection)
            return collection.Count;

        var count = 0;
        foreach (var e in value)
            count++;
        return count;
    }

    /// <summary>
    /// 
    /// </summary>
    /// <param name="value"></param>
    /// <returns></returns>
    public static int GetCount(object? value)
    {
        if (value == null)
            return 0;
        if (value is Array array)
            return array.Length;
        if (value is ICollection collection)
            return collection.Count;

        var count = 0;

        if (value is IEnumerable enumerable)
            foreach (var e in enumerable)
                count++;

        return count;
    }

    /// <summary>
    /// 
    /// </summary>
    /// <param name="value"></param>
    /// <param name="func"></param>
    /// <returns></returns>
    public static int Count(this IEnumerable value, Predicate<object> func)
    {
        var count = 0;
        foreach (var e in value)
            if (func(e))
                count++;
        return count;
    }

    /// <summary>
    /// Get an Array from given IEnumerable
    /// </summary>
    /// <param name="i">Elements</param>
    /// <returns></returns>
    public static Array? CopyToArray(this IEnumerable? i)
    {
        if (i == null)
            return null;
        var list = ToCollection(i);
        var count = list.Count;
        var ga = i.GetType().GetGenericArguments();
        var arr = Array.CreateInstance(ga.Length > 0 ? ga[0] : Typer.TypeObject, count);
        if (count > 0)
            list.CopyTo(arr, 0);
        return arr;
    }

    public static T[]? CopyTo<T>(this Array? value)
    {
        if (value == null)
            return null;

        if (value is T[] dst)
            return dst;

        dst = new T[value.Length];
        Array.Copy(value, 0, dst, 0, value.Length);
        return dst;
    }

    public static IEnumerable<TResult> Select<TResult>(this Array source, Func<object, TResult> selector)
    {
        foreach (var item in source)
            yield return selector(item);

        yield break;
    }

    public static IEnumerable<TResult> Select<T, TResult>(this IEnumerable<T> source, Predicate<T> criteria, Func<object?, TResult> selector)
    {
        foreach (var item in source)
            if (criteria(item))
                yield return selector(item);

        yield break;
    }

    public static string Join<T>(this IEnumerable<T> souce, char separator)
    {
        return string.Join(separator, souce);
    }

    public static string Join<T>(this IEnumerable<T> souce, string separator)
    {
        return string.Join(separator, souce);
    }
}
