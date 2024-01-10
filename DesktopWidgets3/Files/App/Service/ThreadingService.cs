// Copyright (c) 2023 Files Community
// Licensed under the MIT License. See the LICENSE.

using Files.App.Extensions;
using Files.Core.Services;
using Microsoft.UI.Dispatching;

namespace Files.App.Services;

internal sealed class ThreadingService : IThreadingService
{
	private readonly DispatcherQueue _dispatcherQueue;

	public ThreadingService()
	{
        _dispatcherQueue = DesktopWidgets3.App.DispatcherQueue;
	}

	public Task ExecuteOnUiThreadAsync(Action action)
	{
		return _dispatcherQueue.EnqueueOrInvokeAsync(action);
	}

	public Task<TResult?> ExecuteOnUiThreadAsync<TResult>(Func<TResult?> func)
	{
		return _dispatcherQueue.EnqueueOrInvokeAsync<TResult?>(func);
	}
}
