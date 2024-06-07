// Copyright (c) 2024 Files Community
// Licensed under the MIT License. See the LICENSE.

namespace Files.App.Services.SizeProvider;

public sealed class SizeChangedEventArgs(string path, ulong newSize = 0, SizeChangedValueState valueState = SizeChangedValueState.None) : EventArgs
{
    public string Path { get; } = path;
    public ulong NewSize { get; } = newSize;
    public SizeChangedValueState ValueState { get; } = valueState;
}
