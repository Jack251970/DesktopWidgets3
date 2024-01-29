// Copyright (c) 2023 Files Community
// Licensed under the MIT License. See the LICENSE.

using Files.App.Dialogs;
using Files.App.ViewModels.Dialogs;
using Microsoft.Extensions.Logging;
using Microsoft.UI.Xaml.Controls;
using Windows.Foundation.Metadata;

namespace Files.App.Services;

/// <inheritdoc cref="IDialogService"/>
internal sealed class DialogService : IDialogService
{
    private IFolderViewViewModel _folderViewViewModel = null!;

    private IReadOnlyDictionary<Type, Func<ContentDialog>> _dialogs;

	public DialogService()
	{
        _dialogs = new Dictionary<Type, Func<ContentDialog>>() { };
	}

    public void Initialize(IFolderViewViewModel folderViewViewModel)
    {
        _folderViewViewModel = folderViewViewModel;
        _dialogs = new Dictionary<Type, Func<ContentDialog>>()
        {
            { typeof(AddItemDialogViewModel), () => new AddItemDialog() },
            { typeof(CredentialDialogViewModel), () => new CredentialDialog() },
            { typeof(ElevateConfirmDialogViewModel), () => new ElevateConfirmDialog() },
            { typeof(FileSystemDialogViewModel), () => new FilesystemOperationDialog(folderViewViewModel) },
            { typeof(DecompressArchiveDialogViewModel), () => new DecompressArchiveDialog() },
            /*{ typeof(SettingsDialogViewModel), () => new SettingsDialog(folderViewViewModel) },*/
            { typeof(CreateShortcutDialogViewModel), () => new CreateShortcutDialog() },
            { typeof(ReorderSidebarItemsDialogViewModel), () => new ReorderSidebarItemsDialog() },
            { typeof(AddBranchDialogViewModel), () => new AddBranchDialog() },
            { typeof(GitHubLoginDialogViewModel), () => new GitHubLoginDialog() },
            { typeof(FileTooLargeDialogViewModel), () => new FileTooLargeDialog(folderViewViewModel) },
            { typeof(ReleaseNotesDialogViewModel), () => new ReleaseNotesDialog(folderViewViewModel) },
        };
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
            contentDialog.XamlRoot = _folderViewViewModel.MainWindow.Content.XamlRoot;
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
			DependencyExtensions.GetService<ILogger>()?.LogWarning(ex, "Failed to show dialog");

			Debugger.Break();
		}

		return Task.FromResult(DialogResult.None);
	}
}
