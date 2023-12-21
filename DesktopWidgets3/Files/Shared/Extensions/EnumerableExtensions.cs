// Copyright (c) 2023 Files Community
// Licensed under the MIT License. See the LICENSE.

namespace Files.Shared.Extensions;

public static class EnumerableExtensions
{
    public static Task<IList<T>> ToListAsync<T>(this IEnumerable<T> source)
    {
        return Task.Run(() => (IList<T>)source.ToList());
    }
}
