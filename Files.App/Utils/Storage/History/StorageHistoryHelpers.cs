// Copyright (c) 2023 Files Community
// Licensed under the MIT License. See the LICENSE.

namespace Files.App.Utils.Storage;

public class StorageHistoryHelpers : IDisposable
{
    private readonly StorageHistoryWrapper HistoryWrapper = App.HistoryWrapper;

    private IStorageHistoryOperations operations;

	private static readonly SemaphoreSlim semaphore = new(1, 1);

    public StorageHistoryHelpers(IStorageHistoryOperations storageHistoryOperations)
    {
        operations = storageHistoryOperations;
    }

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

        GC.SuppressFinalize(this);
	}
}