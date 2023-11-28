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
}
