// Copyright (c) 2023 Files Community
// Licensed under the MIT License. See the LICENSE.

using System.Collections.Concurrent;

namespace Files.Shared.Extensions;

public static class LinqExtensions
{
    /// <summary>
    /// Determines whether <paramref name="enumerable"/> is empty or not.
    /// <br/><br/>
    /// Remarks:
    /// <br/>
    /// This function is faster than enumerable.Count == 0 since it'll only iterate one element instead of all elements.
    /// <br/>
    /// This function is null-safe.
    /// </summary>
    /// <typeparam name="T"></typeparam>
    /// <param name="enumerable"></param>
    /// <returns></returns>
    public static bool IsEmpty<T>(this IEnumerable<T> enumerable)
    {
        return enumerable is null || !enumerable.Any();
    }

    public static TOut? Get<TOut, TKey, TValue>(this IDictionary<TKey, TValue> dictionary, TKey key, TOut? defaultValue = default) where TKey : notnull
    {
        if (dictionary is null || key is null)
        {
            return defaultValue;
        }

        if (dictionary is ConcurrentDictionary<TKey, TValue> cDict)
        {
            if (!cDict.ContainsKey(key))
            {
                if (defaultValue is TValue value)
                {
                    cDict.TryAdd(key, value);
                }

                return defaultValue;
            }
        }
        else
        {
            lock (dictionary)
            {
                if (!dictionary.ContainsKey(key))
                {
                    if (defaultValue is TValue value)
                    {
                        dictionary.Add(key, value);
                    }

                    return defaultValue;
                }
            }
        }

        return dictionary[key] is TOut o ? o : defaultValue;
    }

    public static Task<TValue?> GetAsync<TKey, TValue>(this IDictionary<TKey, Task<TValue?>> dictionary, TKey key, Func<Task<TValue?>> defaultValueFunc) where TKey : notnull
    {
        if (dictionary is null || key is null)
        {
            return defaultValueFunc();
        }

        if (dictionary is ConcurrentDictionary<TKey, Task<TValue?>> cDict)
        {
            if (!cDict.ContainsKey(key))
            {
                var defaultValue = defaultValueFunc();
                if (defaultValue is Task<TValue?> value)
                {
                    cDict.TryAdd(key, value);
                }

                return defaultValue;
            }
        }
        else
        {
            lock (dictionary)
            {
                if (!dictionary.ContainsKey(key))
                {
                    var defaultValue = defaultValueFunc();
                    if (defaultValue is Task<TValue?> value)
                    {
                        dictionary.Add(key, value);
                    }

                    return defaultValue;
                }
            }
        }

        return dictionary[key];
    }


    /// <summary>
    /// Enumerates through <see cref="IEnumerable{T}"/> of elements and executes <paramref name="action"/>
    /// </summary>
    /// <typeparam name="T">Element of <paramref name="collection"/></typeparam>
    /// <param name="collection">The collection to enumerate through</param>
    /// <param name="action">The action to take every element</param>
    public static void ForEach<T>(this IEnumerable<T> collection, Action<T> action)
    {
        foreach (var value in collection)
        {
            action(value);
        }
    }
}
