// Copyright (c) 2023 Files Community
// Licensed under the MIT License. See the LICENSE.

using Microsoft.UI.Xaml.Controls;
using System.Text.RegularExpressions;
using Vanara.PInvoke;
using Windows.Foundation.Metadata;
using Windows.Storage;

namespace Files.App.Utils.RecycleBin;

public static partial class RecycleBinHelpers
{
    /*private static readonly StatusCenterViewModel _statusCenterViewModel = DependencyExtensions.GetService<StatusCenterViewModel>();*/

    private static readonly Regex recycleBinPathRegex = MyRegex();

    /*private static readonly IUserSettingsService userSettingsService = DependencyExtensions.GetService<IUserSettingsService>();*/

    public static async Task<List<ShellFileItem>> EnumerateRecycleBin()
	{
		return (await Win32Shell.GetShellFolderAsync(Constants.UserEnvironmentPaths.RecycleBinPath, "Enumerate", 0, int.MaxValue)).Enumerate;
	}

	public static ulong GetSize()
	{
		return (ulong)Win32Shell.QueryRecycleBin().BinSize;
	}

	public static async Task<bool> IsRecycleBinItem(IStorageItem item)
	{
		var recycleBinItems = await EnumerateRecycleBin();
		return recycleBinItems.Any((shellItem) => shellItem.RecyclePath == item.Path);
	}

	public static async Task<bool> IsRecycleBinItem(string path)
	{
		var recycleBinItems = await EnumerateRecycleBin();
		return recycleBinItems.Any((shellItem) => shellItem.RecyclePath == path);
	}

	public static bool IsPathUnderRecycleBin(string path)
	{
		return !string.IsNullOrWhiteSpace(path) && recycleBinPathRegex.IsMatch(path);
	}

    public static async Task EmptyRecycleBinAsync(IFolderViewViewModel folderViewViewModel)
	{
		// Display confirmation dialog
		var ConfirmEmptyBinDialog = new ContentDialog()
		{
			Title = "ConfirmEmptyBinDialogTitle".ToLocalized(),
			Content = "ConfirmEmptyBinDialogContent".ToLocalized(),
			PrimaryButtonText = "Yes".ToLocalized(),
			SecondaryButtonText = "Cancel".ToLocalized(),
			DefaultButton = ContentDialogButton.Primary
		};

		if (ApiInformation.IsApiContractPresent("Windows.Foundation.UniversalApiContract", 8))
        {
            ConfirmEmptyBinDialog.XamlRoot = folderViewViewModel.MainWindow.Content.XamlRoot;
        }

        var _statusCenterViewModel = folderViewViewModel.GetService<StatusCenterViewModel>();
        var userSettingsService = folderViewViewModel.GetService<IUserSettingsService>();
        // If the operation is approved by the user
        if (userSettingsService.FoldersSettingsService.DeleteConfirmationPolicy is DeleteConfirmationPolicies.Never ||
			await ConfirmEmptyBinDialog.TryShowAsync(folderViewViewModel) == ContentDialogResult.Primary)
		{

			var banner = StatusCenterHelper.AddCard_EmptyRecycleBin(folderViewViewModel, ReturnResult.InProgress);

			var bResult = await Task.Run(() => Shell32.SHEmptyRecycleBin(nint.Zero, null, Shell32.SHERB.SHERB_NOCONFIRMATION | Shell32.SHERB.SHERB_NOPROGRESSUI).Succeeded);

			_statusCenterViewModel.RemoveItem(banner);

			if (bResult)
            {
                StatusCenterHelper.AddCard_EmptyRecycleBin(folderViewViewModel, ReturnResult.Success);
            }
            else
            {
                StatusCenterHelper.AddCard_EmptyRecycleBin(folderViewViewModel, ReturnResult.Failed);
            }
        }
	}

    public static async Task RestoreRecycleBinAsync(IFolderViewViewModel folderViewViewModel)
	{
		var confirmEmptyBinDialog = new ContentDialog()
		{
			Title = "ConfirmRestoreBinDialogTitle".ToLocalized(),
			Content = "ConfirmRestoreBinDialogContent".ToLocalized(),
			PrimaryButtonText = "Yes".ToLocalized(),
			SecondaryButtonText = "Cancel".ToLocalized(),
			DefaultButton = ContentDialogButton.Primary
		};

		if (ApiInformation.IsApiContractPresent("Windows.Foundation.UniversalApiContract", 8))
        {
            confirmEmptyBinDialog.XamlRoot = folderViewViewModel.MainWindow.Content.XamlRoot;
        }

        var result = await confirmEmptyBinDialog.TryShowAsync(folderViewViewModel);

		if (result == ContentDialogResult.Primary)
		{
			try
			{
				Vanara.Windows.Shell.RecycleBin.RestoreAll();
			}
			catch (Exception)
			{
				var errorDialog = new ContentDialog()
				{
					Title = "FailedToRestore".ToLocalized(),
					PrimaryButtonText = "OK".ToLocalized(),
				};

				if (ApiInformation.IsApiContractPresent("Windows.Foundation.UniversalApiContract", 8))
                {
                    errorDialog.XamlRoot = folderViewViewModel.MainWindow.Content.XamlRoot;
                }

                await errorDialog.TryShowAsync(folderViewViewModel);
			}
		}
	}

	public static async Task RestoreSelectionRecycleBinAsync(IFolderViewViewModel folderViewViewModel, IShellPage associatedInstance)
	{
		var items = associatedInstance.SlimContentPage.SelectedItems;
		if (items == null)
        {
            return;
        }

        var ConfirmEmptyBinDialog = new ContentDialog()
		{
			Title = "ConfirmRestoreSelectionBinDialogTitle".ToLocalized(),
			Content = string.Format("ConfirmRestoreSelectionBinDialogContent".ToLocalized(), items.Count),
			PrimaryButtonText = "Yes".ToLocalized(),
			SecondaryButtonText = "Cancel".ToLocalized(),
			DefaultButton = ContentDialogButton.Primary
		};

		if (ApiInformation.IsApiContractPresent("Windows.Foundation.UniversalApiContract", 8))
        {
            ConfirmEmptyBinDialog.XamlRoot = folderViewViewModel.MainWindow.Content.XamlRoot;
        }

        var result = await ConfirmEmptyBinDialog.TryShowAsync(folderViewViewModel);

		if (result == ContentDialogResult.Primary)
        {
            await RestoreItemAsync(associatedInstance);
        }
    }

    public static async Task<bool> HasRecycleBin(string? path)
	{
		if (string.IsNullOrEmpty(path) || path.StartsWith(@"\\?\", StringComparison.Ordinal))
        {
            return false;
        }

        var result = await FileOperationsHelpers.TestRecycleAsync(path.Split('|'));

		return result.Item1 &= result.Item2 is not null && result.Item2.Items.All(x => x.Succeeded);
	}

	public static bool RecycleBinHasItems()
	{
		return Win32Shell.QueryRecycleBin().NumItems > 0;
	}

	public static async Task RestoreItemAsync(IShellPage associatedInstance)
	{
		var selected = associatedInstance.SlimContentPage.SelectedItems;
		if (selected == null)
        {
            return;
        }

        var items = selected.ToList().Where(x => x is RecycleBinItem).Select((item) => new
		{
			Source = StorageHelpers.FromPathAndType(
				item.ItemPath,
				item.PrimaryItemAttribute == StorageItemTypes.File ? FilesystemItemType.File : FilesystemItemType.Directory),
			Dest = ((RecycleBinItem)item).ItemOriginalPath
		});
		await associatedInstance.FilesystemHelpers.RestoreItemsFromTrashAsync(items.Select(x => x.Source), items.Select(x => x.Dest), true);
	}

	public static async Task DeleteItemAsync(IFolderViewViewModel folderViewViewModel, IShellPage associatedInstance)
	{
		var selected = associatedInstance.SlimContentPage.SelectedItems;
		if (selected == null)
        {
            return;
        }

        var userSettingsService = folderViewViewModel.GetService<IUserSettingsService>();
        var items = selected.ToList().Select((item) => StorageHelpers.FromPathAndType(
			item.ItemPath,
			item.PrimaryItemAttribute == StorageItemTypes.File ? FilesystemItemType.File : FilesystemItemType.Directory));
		await associatedInstance.FilesystemHelpers.DeleteItemsAsync(items, userSettingsService.FoldersSettingsService.DeleteConfirmationPolicy, false, true);
	}

    [GeneratedRegex("^[A-Z]:\\\\\\$Recycle\\.Bin\\\\", RegexOptions.IgnoreCase | RegexOptions.CultureInvariant)]
    private static partial Regex MyRegex();
}
