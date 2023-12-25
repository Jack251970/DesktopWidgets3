// Copyright (c) 2023 Files Community
// Licensed under the MIT License. See the LICENSE.

using DesktopWidgets3.ViewModels.Pages.Widget;
using Files.App.Helpers;
using Files.App.ViewModels.Dialogs;
using Files.Core.Data.Enums;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Input;

namespace Files.App.Dialogs;

public sealed partial class DynamicDialog : ContentDialog, IDisposable
{
    public DynamicDialogViewModel ViewModel
    {
        get => (DynamicDialogViewModel)DataContext;
        private set => DataContext = value;
    }

    public DynamicDialogResult DynamicResult => ViewModel.DynamicResult;

    public Task<ContentDialogResult> ShowAsync(FolderViewViewModel viewModel)
    {
        return this.TryShowAsync(viewModel);
    }

    public DynamicDialog(DynamicDialogViewModel dynamicDialogViewModel)
    {
        InitializeComponent();

        dynamicDialogViewModel.HideDialog = Hide;
        ViewModel = dynamicDialogViewModel;
    }

    private void ContentDialog_PrimaryButtonClick(ContentDialog sender, ContentDialogButtonClickEventArgs args)
    {
        ViewModel.PrimaryButtonCommand.Execute(args);
    }

    private void ContentDialog_SecondaryButtonClick(ContentDialog sender, ContentDialogButtonClickEventArgs args)
    {
        ViewModel.SecondaryButtonCommand.Execute(args);
    }

    private void ContentDialog_CloseButtonClick(ContentDialog sender, ContentDialogButtonClickEventArgs args)
    {
        ViewModel.CloseButtonCommand.Execute(args);
    }

    private void ContentDialog_KeyDown(object sender, KeyRoutedEventArgs e)
    {
        ViewModel.KeyDownCommand.Execute(e);
    }

    public void Dispose()
    {
        ViewModel?.Dispose();
        ViewModel = null!;
    }
}
