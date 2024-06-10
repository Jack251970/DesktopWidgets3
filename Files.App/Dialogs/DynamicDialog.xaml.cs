// Copyright (c) 2024 Files Community
// Licensed under the MIT License. See the LICENSE.

using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;

namespace Files.App.Dialogs;

public sealed partial class DynamicDialog : ContentDialog, IDisposable
{
    private readonly IFolderViewViewModel FolderViewViewModel;

    private FrameworkElement RootAppElement
        => (FrameworkElement)FolderViewViewModel.Content;

    public DynamicDialogViewModel ViewModel
	{
		get => (DynamicDialogViewModel)DataContext;
		private set => DataContext = value;
	}

    public DynamicDialogResult DynamicResult => ViewModel.DynamicResult;

    public Task<ContentDialogResult> ShowAsync(IFolderViewViewModel viewModel)
	{
		return this.TryShowAsync(viewModel);
	}

	public DynamicDialog(IFolderViewViewModel folderViewViewModel, DynamicDialogViewModel dynamicDialogViewModel)
	{
        FolderViewViewModel = folderViewViewModel;

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

	private void ContentDialog_KeyDown(object sender, Microsoft.UI.Xaml.Input.KeyRoutedEventArgs e)
	{
		ViewModel.KeyDownCommand.Execute(e);
	}

	public void Dispose()
	{
		ViewModel?.Dispose();
		ViewModel = null!;
	}
}
