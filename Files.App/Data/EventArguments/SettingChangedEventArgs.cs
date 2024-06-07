// Copyright (c) 2024 Files Community
// Licensed under the MIT License. See the LICENSE.

namespace Files.App.Data.EventArguments;

public sealed class SettingChangedEventArgs(string settingName, object? newValue) : EventArgs
{
    public string SettingName { get; } = settingName;

    public object? NewValue { get; } = newValue;
}
