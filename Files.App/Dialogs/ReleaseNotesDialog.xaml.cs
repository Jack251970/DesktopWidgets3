// Copyright (c) 2024 Files Community
// Licensed under the MIT License. See the LICENSE.

using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Windows.System;

namespace Files.App.Dialogs;

public sealed partial class ReleaseNotesDialog : ContentDialog, IDialog<ReleaseNotesDialogViewModel>
{
    private readonly IFolderViewViewModel FolderViewViewModel;

    private FrameworkElement RootAppElement
        => (FrameworkElement)FolderViewViewModel.Content;

    public ReleaseNotesDialogViewModel ViewModel
	{
		get => (ReleaseNotesDialogViewModel)DataContext;
		set => DataContext = value;
	}

	public ReleaseNotesDialog(IFolderViewViewModel folderViewViewModel)
	{
        FolderViewViewModel = folderViewViewModel;

        InitializeComponent();

        FolderViewViewModel.MainWindow.SizeChanged += Current_SizeChanged;
		UpdateDialogLayout();
	}

	private void UpdateDialogLayout()
	{
		ContainerGrid.MaxHeight = FolderViewViewModel.Bounds.Height - 70;
	}

	private void Current_SizeChanged(object sender, WindowSizeChangedEventArgs e)
	{
		UpdateDialogLayout();
	}

	private void ContentDialog_Closing(ContentDialog sender, ContentDialogClosingEventArgs args)
	{
        FolderViewViewModel.MainWindow.SizeChanged -= Current_SizeChanged;
	}

	public async new Task<DialogResult> ShowAsync()
	{
		return (DialogResult)await SetContentDialogRoot(this).TryShowAsync(FolderViewViewModel);
	}

	private void CloseDialogButton_Click(object sender, RoutedEventArgs e)
	{
		Hide();
	}

	// WINUI3
	private ContentDialog SetContentDialogRoot(ContentDialog contentDialog)
	{
		if (Windows.Foundation.Metadata.ApiInformation.IsApiContractPresent("Windows.Foundation.UniversalApiContract", 8))
        {
            contentDialog.XamlRoot = FolderViewViewModel.XamlRoot;
        }

        return contentDialog;
	}

    private async void ReleaseNotesMarkdownTextBlock_LinkClicked(object sender, CommunityToolkit.WinUI.UI.Controls.LinkClickedEventArgs e)
    {
        if (Uri.TryCreate(e.Link, UriKind.Absolute, out var link))
        {
            await Launcher.LaunchUriAsync(link);
        }
    }
}
