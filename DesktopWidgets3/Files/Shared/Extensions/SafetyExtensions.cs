// Copyright (c) 2023 Files Community
// Licensed under the MIT License. See the LICENSE.

namespace Files.Shared.Extensions;

public class SafetyExtensions
{
    public static T? IgnoreExceptions<T>(Func<T> action)
    {
        try
        {
            return action();
        }
        catch (Exception)
        {
            return default;
        }
    }

    public static async Task<TOut> Wrap<TOut>(Func<Task<TOut>> inputTask, Func<Func<Task<TOut>>, Exception, Task<TOut>> onFailed)
    {
        try
        {
            return await inputTask();
        }
        catch (Exception ex)
        {
            return await onFailed(inputTask, ex);
        }
    }

    public static async Task WrapAsync(Func<Task> inputTask, Func<Func<Task>, Exception, Task> onFailed)
    {
        try
        {
            await inputTask();
        }
        catch (Exception ex)
        {
            await onFailed(inputTask, ex);
        }
    }
}
