// Copyright (c) 2024 Files Community
// Licensed under the MIT License. See the LICENSE.

using System.Collections.Frozen;
using Files.App.Dialogs;
using Microsoft.Extensions.Logging;
using Microsoft.UI.Xaml.Controls;
using Windows.Foundation.Metadata;

namespace Files.App.Services;

/// <inheritdoc cref="IDialogService"/>
internal sealed class DialogService : IDialogService
{
    private IFolderViewViewModel _folderViewViewModel = null!;

    private FrozenDictionary<Type, Func<ContentDialog>> _dialogs;

	public DialogService()
	{
        _dialogs = new Dictionary<Type, Func<ContentDialog>>() { }.ToFrozenDictionary();
	}

    public void Initialize(IFolderViewViewModel folderViewViewModel)
    {
        _folderViewViewModel = folderViewViewModel;
        _dialogs = new Dictionary<Type, Func<ContentDialog>>()
        {
            { typeof(AddItemDialogViewModel), () => new AddItemDialog(folderViewViewModel) },
            { typeof(CredentialDialogViewModel), () => new CredentialDialog(folderViewViewModel) },
            { typeof(ElevateConfirmDialogViewModel), () => new ElevateConfirmDialog(folderViewViewModel) },
            { typeof(FileSystemDialogViewModel), () => new FilesystemOperationDialog(folderViewViewModel) },
            { typeof(DecompressArchiveDialogViewModel), () => new DecompressArchiveDialog(folderViewViewModel) },
            { typeof(SettingsDialogViewModel), () => new SettingsDialog(folderViewViewModel) },
            { typeof(CreateShortcutDialogViewModel), () => new CreateShortcutDialog(folderViewViewModel) },
            { typeof(ReorderSidebarItemsDialogViewModel), () => new ReorderSidebarItemsDialog(folderViewViewModel) },
            { typeof(AddBranchDialogViewModel), () => new AddBranchDialog(folderViewViewModel) },
            { typeof(GitHubLoginDialogViewModel), () => new GitHubLoginDialog(folderViewViewModel) },
            { typeof(FileTooLargeDialogViewModel), () => new FileTooLargeDialog(folderViewViewModel) },
            { typeof(ReleaseNotesDialogViewModel), () => new ReleaseNotesDialog(folderViewViewModel) },
        }.ToFrozenDictionary();
    }

    /// <inheritdoc/>
    public IDialog<TViewModel> GetDialog<TViewModel>(TViewModel viewModel)
		where TViewModel : class, INotifyPropertyChanged
	{
		if (!_dialogs.TryGetValue(typeof(TViewModel), out var initializer))
        {
            throw new ArgumentException($"{typeof(TViewModel)} does not have an appropriate dialog associated with it.");
        }

        var contentDialog = initializer();
		if (contentDialog is not IDialog<TViewModel> dialog)
        {
            throw new NotSupportedException($"The dialog does not implement {typeof(IDialog<TViewModel>)}.");
        }

        dialog.ViewModel = viewModel;

		if (ApiInformation.IsApiContractPresent("Windows.Foundation.UniversalApiContract", 8))
        {
            contentDialog.XamlRoot = _folderViewViewModel.XamlRoot;
        }

        return dialog;
	}

	/// <inheritdoc/>
	public Task<DialogResult> ShowDialogAsync<TViewModel>(TViewModel viewModel)
		where TViewModel : class, INotifyPropertyChanged
	{
		try
		{
			return GetDialog(viewModel).TryShowAsync(_folderViewViewModel);
		}
		catch (Exception ex)
		{
			LogExtensions.LogWarning(ex, "Failed to show dialog");

			Debugger.Break();
		}

		return Task.FromResult(DialogResult.None);
	}
}
