// Copyright (c) 2024 Files Community
// Licensed under the MIT License. See the LICENSE.

namespace Files.App.Utils.Storage;

#pragma warning disable CA1816 // Dispose methods should call SuppressFinalize

public sealed class StorageHistoryHelpers(IStorageHistoryOperations storageHistoryOperations) : IDisposable
{
    private readonly StorageHistoryWrapper HistoryWrapper = App.HistoryWrapper;

    private IStorageHistoryOperations operations = storageHistoryOperations;

	private static readonly SemaphoreSlim semaphore = new(1, 1);

    public async Task<ReturnResult> TryUndo()
	{
		if (HistoryWrapper.CanUndo())
		{
			if (!await semaphore.WaitAsync(0))
			{
				return ReturnResult.InProgress;
			}
			var keepHistory = false;
			try
			{
				var result = await operations.Undo(HistoryWrapper.GetCurrentHistory());
				keepHistory = result is ReturnResult.Cancelled;
				return result;
			}
			finally
			{
				if (!keepHistory)
                {
                    HistoryWrapper.DecreaseIndex();
                }

                semaphore.Release();
			}
		}

		return ReturnResult.Cancelled;
	}

	public async Task<ReturnResult> TryRedo()
	{
		if (HistoryWrapper.CanRedo())
		{
			if (!await semaphore.WaitAsync(0))
			{
				return ReturnResult.InProgress;
			}
			try
			{
				HistoryWrapper.IncreaseIndex();
				return await operations.Redo(HistoryWrapper.GetCurrentHistory());
			}
			finally
			{
				semaphore.Release();
			}
		}

		return ReturnResult.Cancelled;
	}

	public void Dispose()
	{
		operations?.Dispose();
		operations = null!;
	}
}