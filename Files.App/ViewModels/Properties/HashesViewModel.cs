// Copyright (c) 2024 Files Community
// Licensed under the MIT License. See the LICENSE.

using Files.Shared.Helpers;
using System.IO;
using System.Windows.Input;

namespace Files.App.ViewModels.Properties;

#pragma warning disable CA1816 // Dispose methods should call SuppressFinalize

public sealed class HashesViewModel : ObservableObject, IDisposable
{
    private IUserSettingsService UserSettingsService { get; set; } = null!;

	private HashInfoItem _selectedItem = null!;
	public HashInfoItem SelectedItem
	{
		get => _selectedItem;
		set => SetProperty(ref _selectedItem, value);
	}

	public ObservableCollection<HashInfoItem> Hashes { get; set; }

	public Dictionary<string, bool> ShowHashes { get; private set; }

	public ICommand ToggleIsEnabledCommand { get; private set; }

	private readonly ListedItem _item;

	private readonly CancellationTokenSource _cancellationTokenSource;

	public HashesViewModel(IFolderViewViewModel folderViewViewModel, ListedItem item)
	{
        UserSettingsService = folderViewViewModel.GetRequiredService<IUserSettingsService>();

		ToggleIsEnabledCommand = new RelayCommand<string>(ToggleIsEnabled);

		_item = item;
		_cancellationTokenSource = new();

		Hashes =
        [
            new() { Algorithm = "CRC32" },
			new() { Algorithm = "MD5" },
			new() { Algorithm = "SHA1" },
			new() { Algorithm = "SHA256" },
			new() { Algorithm = "SHA384" },
			new() { Algorithm = "SHA512" },
		];

        ShowHashes = UserSettingsService.GeneralSettingsService.ShowHashesDictionary ?? [];
        // Default settings
        ShowHashes.TryAdd("CRC32", true);
		ShowHashes.TryAdd("MD5", true);
		ShowHashes.TryAdd("SHA1", true);
		ShowHashes.TryAdd("SHA256", true);
		ShowHashes.TryAdd("SHA384", false);
		ShowHashes.TryAdd("SHA512", false);

		Hashes.Where(x => ShowHashes[x.Algorithm]).ForEach(x => ToggleIsEnabledCommand.Execute(x.Algorithm));
	}

	private void ToggleIsEnabled(string? algorithm)
	{
        var hashInfoItem = Hashes.First(x => x.Algorithm == algorithm);
        hashInfoItem.IsEnabled = !hashInfoItem.IsEnabled;

		if (ShowHashes[hashInfoItem.Algorithm] != hashInfoItem.IsEnabled)
		{
			ShowHashes[hashInfoItem.Algorithm] = hashInfoItem.IsEnabled;
			UserSettingsService.GeneralSettingsService.ShowHashesDictionary = ShowHashes;
		}

		// Don't calculate hashes for online files
		if (_item.SyncStatusUI.SyncStatus is CloudDriveSyncStatus.FileOnline or CloudDriveSyncStatus.FolderOnline)
		{
			hashInfoItem.HashValue = "CalculationOnlineFileHashError".GetLocalizedResource();
			return;
		}

		if (hashInfoItem.HashValue is null && hashInfoItem.IsEnabled)
		{
			hashInfoItem.IsCalculating = true;

			ThreadExtensions.MainDispatcherQueue.EnqueueOrInvokeAsync(async () =>
			{
				try
				{
					using (var stream = File.OpenRead(_item.ItemPath))
					{
						hashInfoItem.HashValue = hashInfoItem.Algorithm switch
						{
							"CRC32" => await ChecksumHelpers.CreateCRC32(stream, _cancellationTokenSource.Token),
							"MD5" => await ChecksumHelpers.CreateMD5(stream, _cancellationTokenSource.Token),
							"SHA1" => await ChecksumHelpers.CreateSHA1(stream, _cancellationTokenSource.Token),
							"SHA256" => await ChecksumHelpers.CreateSHA256(stream, _cancellationTokenSource.Token),
							"SHA384" => await ChecksumHelpers.CreateSHA384(stream, _cancellationTokenSource.Token),
							"SHA512" => await ChecksumHelpers.CreateSHA512(stream, _cancellationTokenSource.Token),
							_ => throw new InvalidOperationException()
						};
					}

					hashInfoItem.IsCalculated = true;
				}
				catch (OperationCanceledException)
				{
					// not an error
				}
				catch (IOException)
				{
					// File is currently open
					hashInfoItem.HashValue = "CalculationErrorFileIsOpen".GetLocalizedResource();
				}
				catch (Exception)
				{
					hashInfoItem.HashValue = "CalculationError".GetLocalizedResource();
				}
				finally
				{
					hashInfoItem.IsCalculating = false;
				}
			});
		}
	}

	public void Dispose()
	{
		_cancellationTokenSource.Cancel();
	}
}
