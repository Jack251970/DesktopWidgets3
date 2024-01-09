// Copyright (c) 2023 Files Community
// Licensed under the MIT License. See the LICENSE.

using System.ComponentModel;
using DesktopWidgets3.Files.Core.Data.Enums;
using DesktopWidgets3.Files.Core.Services;
using DesktopWidgets3.Files.Core.ViewModels.Dialogs;
using DesktopWidgets3.Files.Core.ViewModels.Dialogs.FileSystemDialog;
using Microsoft.UI.Xaml.Controls;
using Windows.Foundation.Metadata;
using DesktopWidgets3.ViewModels.Pages.Widget;
using DesktopWidgets3.Files.App.Helpers;
using DesktopWidgets3.Files.App.Dialogs;

namespace DesktopWidgets3.Files.App.Services;

/// <inheritdoc cref="IDialogService"/>
internal sealed class DialogService : IDialogService
{
    private IReadOnlyDictionary<Type, Func<ContentDialog>> _dialogs;

    private FolderViewViewModel _folderViewModel = null!;

    public DialogService()
    {
        _dialogs = new Dictionary<Type, Func<ContentDialog>>() {};
    }

    public void Initialize(FolderViewViewModel folderViewModel)
    {
        _folderViewModel = folderViewModel;
        _dialogs = new Dictionary<Type, Func<ContentDialog>>()
        {
            /*{ typeof(AddItemDialogViewModel), () => new AddItemDialog() },
            { typeof(CredentialDialogViewModel), () => new CredentialDialog() },
            { typeof(ElevateConfirmDialogViewModel), () => new ElevateConfirmDialog() },*/
            { typeof(FileSystemDialogViewModel), () => new FileSystemOperationDialog(folderViewModel) },
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
