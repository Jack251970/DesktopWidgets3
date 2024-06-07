﻿// Copyright (c) 2024 Files Community
// Licensed under the MIT License. See the LICENSE.

using Files.Shared.Helpers;
using Windows.ApplicationModel.DataTransfer;

namespace Files.App.Data.EventArguments;

public sealed class PathBoxItemDroppedEventArgs
{
	public DataPackageView Package { get; set; } = null!;

    public string Path { get; set; } = null!;

    public DataPackageOperation AcceptedOperation { get; set; }

	public AsyncManualResetEvent SignalEvent { get; set; } = null!;
}
