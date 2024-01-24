// Copyright (c) 2023 Files Community
// Licensed under the MIT License. See the LICENSE.

using System.ComponentModel;
using Files.Core.Data.Enums;
using Files.Core.Services;
using Files.Core.ViewModels.Dialogs;
using Files.Core.ViewModels.Dialogs.FileSystemDialog;
using Microsoft.UI.Xaml.Controls;
using Windows.Foundation.Metadata;
using Files.App.Helpers;
using Files.App.Dialogs;
using DesktopWidgets3.ViewModels.Pages.Widget;
using Files.Core.ViewModels.Widgets.FolderView;

namespace Files.App.Services;

/// <inheritdoc cref="IDialogService"/>
internal sealed class DialogService : IDialogService
{
    private IReadOnlyDictionary<Type, Func<ContentDialog>> _dialogs;

    private FolderViewViewModel _folderViewModel = null!;

    public DialogService()
    {
        _dialogs = new Dictionary<Type, Func<ContentDialog>>() {};
    }

    public void Initialize(IFolderViewViewModel folderViewModel)
    {
        _folderViewModel = (FolderViewViewModel)folderViewModel;
        _dialogs = new Dictionary<Type, Func<ContentDialog>>()
        {
            /*{ typeof(AddItemDialogViewModel), () => new AddItemDialog() },
            { typeof(CredentialDialogViewModel), () => new CredentialDialog() },
            { typeof(ElevateConfirmDialogViewModel), () => new ElevateConfirmDialog() },*/
            { typeof(FileSystemDialogViewModel), () => new FilesystemOperationDialog(_folderViewModel) },
            /*{ typeof(DecompressArchiveDialogViewModel), () => new DecompressArchiveDialog() },
            { typeof(SettingsDialogViewModel), () => new SettingsDialog() },
            { typeof(CreateShortcutDialogViewModel), () => new CreateShortcutDialog() },
            { typeof(ReorderSidebarItemsDialogViewModel), () => new ReorderSidebarItemsDialog() },
            { typeof(AddBranchDialogViewModel), () => new AddBranchDialog() },
            { typeof(GitHubLoginDialogViewModel), () => new GitHubLoginDialog() },
            { typeof(FileTooLargeDialogViewModel), () => new FileTooLargeDialog() },
            { typeof(ReleaseNotesDialogViewModel), () => new ReleaseNotesDialog() },*/
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
            contentDialog.XamlRoot = _folderViewModel.WidgetWindow.Content.XamlRoot;
        }

        return dialog;
    }

    /// <inheritdoc/>
    public Task<DialogResult> ShowDialogAsync<TViewModel>(TViewModel viewModel)
        where TViewModel : class, INotifyPropertyChanged
    {
        try
        {
            return GetDialog(viewModel).TryShowAsync(_folderViewModel);
        }
        catch (Exception)
        {

        }

        return Task.FromResult(DialogResult.None);
    }
}
