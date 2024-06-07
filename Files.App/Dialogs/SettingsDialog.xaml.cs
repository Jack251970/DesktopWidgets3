// Copyright (c) 2024 Files Community
// Licensed under the MIT License. See the LICENSE.

using Files.App.Views.Settings;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;

namespace Files.App.Dialogs;

public sealed partial class SettingsDialog : ContentDialog, IDialog<SettingsDialogViewModel>
{
    private readonly IFolderViewViewModel FolderViewViewModel;

    public SettingsDialogViewModel ViewModel { get; set; } = null!;

	private FrameworkElement RootAppElement
		=> (FrameworkElement)FolderViewViewModel.Content;

	public SettingsDialog(IFolderViewViewModel folderViewViewModel)
	{
        FolderViewViewModel = folderViewViewModel;

        InitializeComponent();
        
        FolderViewViewModel.MainWindow.SizeChanged += Current_SizeChanged;
		UpdateDialogLayout();
	}

	public async new Task<DialogResult> ShowAsync()
	{
		return (DialogResult)await base.ShowAsync();
	}

	private void Current_SizeChanged(object sender, WindowSizeChangedEventArgs e)
	{
		UpdateDialogLayout();
	}

	private void UpdateDialogLayout()
	{
		ContainerGrid.Height = FolderViewViewModel.Bounds.Height <= 760 ? FolderViewViewModel.Bounds.Height - 70 : 690;
		ContainerGrid.Width = FolderViewViewModel.Bounds.Width <= 1100 ? FolderViewViewModel.Bounds.Width : 1100;
		MainSettingsNavigationView.PaneDisplayMode = FolderViewViewModel.Bounds.Width < 700 ? NavigationViewPaneDisplayMode.LeftCompact : NavigationViewPaneDisplayMode.Left;
	}

	private void MainSettingsNavigationView_SelectionChanged(NavigationView sender, NavigationViewSelectionChangedEventArgs args)
	{
		var selectedItem = (NavigationViewItem)args.SelectedItem;

        // CHANGE: Initialize folder view view model through navigation parameter.
        _ = Enum.Parse<SettingsPageKind>(selectedItem.Tag.ToString()!) switch
        {
            SettingsPageKind.GeneralPage => SettingsContentFrame.Navigate(typeof(GeneralPage), FolderViewViewModel),
            SettingsPageKind.AppearancePage => SettingsContentFrame.Navigate(typeof(AppearancePage), FolderViewViewModel),
            SettingsPageKind.LayoutPage => SettingsContentFrame.Navigate(typeof(LayoutPage), FolderViewViewModel),
            SettingsPageKind.FoldersPage => SettingsContentFrame.Navigate(typeof(FoldersPage), FolderViewViewModel),
            SettingsPageKind.ActionsPage => SettingsContentFrame.Navigate(typeof(ActionsPage), FolderViewViewModel),
            SettingsPageKind.TagsPage => SettingsContentFrame.Navigate(typeof(TagsPage), FolderViewViewModel),
            SettingsPageKind.GitPage => SettingsContentFrame.Navigate(typeof(GitPage), FolderViewViewModel),
            SettingsPageKind.AdvancedPage => SettingsContentFrame.Navigate(typeof(AdvancedPage), FolderViewViewModel),
            SettingsPageKind.AboutPage => SettingsContentFrame.Navigate(typeof(AboutPage), FolderViewViewModel),
            _ => SettingsContentFrame.Navigate(typeof(AppearancePage), FolderViewViewModel)
        };
    }

	private void ContentDialog_Closing(ContentDialog sender, ContentDialogClosingEventArgs args)
	{
        FolderViewViewModel.MainWindow.SizeChanged -= Current_SizeChanged;
	}

	private void CloseSettingsDialogButton_Click(object sender, RoutedEventArgs e)
	{
		Hide();
	}
}
