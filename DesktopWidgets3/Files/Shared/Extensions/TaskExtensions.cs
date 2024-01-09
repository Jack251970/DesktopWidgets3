// Copyright (c) 2023 Files Community
// Licensed under the MIT License. See the LICENSE.

namespace DesktopWidgets3.Files.Shared.Extensions;

public static class TaskExtensions
{
    public static async Task WithTimeoutAsync(this Task task, TimeSpan timeout)
    {
        if (task == await Task.WhenAny(task, Task.Delay(timeout)))
        {
            await task;
        }
    }

    public static async Task<T?> WithTimeoutAsync<T>(this Task<T> task, TimeSpan timeout, T? defaultValue = default)
    {
        return task == await Task.WhenAny(task, Task.Delay(timeout)) ? await task : defaultValue;
    }

    public static async Task<TOut> AndThen<TIn, TOut>(this Task<TIn> inputTask, Func<TIn, Task<TOut>> mapping)
    {
        var input = await inputTask;

        return (await mapping(input));
    }
}