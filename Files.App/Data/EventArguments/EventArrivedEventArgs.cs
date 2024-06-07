// Copyright (c) 2024 Files Community
// Licensed under the MIT License. See the LICENSE.

using Microsoft.Management.Infrastructure;

namespace Files.App.Data.EventArguments;

/// <summary>
/// CimWatcher event args, which contains CimSubscriptionResult
/// </summary>
public sealed class EventArrivedEventArgs(CimSubscriptionResult cimSubscriptionResult) : EventArgs
{
    public CimSubscriptionResult NewEvent { get; } = cimSubscriptionResult;
}
