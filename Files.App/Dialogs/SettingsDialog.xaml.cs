// Copyright (c) 2023 Files Community
// Licensed under the MIT License. See the LICENSE.

using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;

namespace Files.App.Dialogs;

public sealed partial class SettingsDialog : ContentDialog, IDialog<SettingsDialogViewModel>
{
    private readonly IFolderViewViewModel FolderViewViewModel;

    public SettingsDialogViewModel ViewModel { get; set; } = null!;

	private FrameworkElement RootAppElement
		=> (FrameworkElement)FolderViewViewModel.MainWindow.Content;

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
		ContainerGrid.Height = FolderViewViewModel.MainWindow.Bounds.Height <= 760 ? FolderViewViewModel.MainWindow.Bounds.Height - 70 : 690;
		ContainerGrid.Width = FolderViewViewModel.MainWindow.Bounds.Width <= 1100 ? FolderViewViewModel.MainWindow.Bounds.Width : 1100;
		MainSettingsNavigationView.PaneDisplayMode = FolderViewViewModel.MainWindow.Bounds.Width < 700 ? NavigationViewPaneDisplayMode.LeftCompact : NavigationViewPaneDisplayMode.Left;
	}

	private void MainSettingsNavigationView_SelectionChanged(NavigationView sender, NavigationViewSelectionChangedEventArgs args)
	{
		var selectedItem = (NavigationViewItem)args.SelectedItem;
		var selectedItemTag = Convert.ToInt32(selectedItem.Tag);

		/*_ = selectedItemTag switch
		{
			0 => SettingsContentFrame.Navigate(typeof(GeneralPage)),
			1 => SettingsContentFrame.Navigate(typeof(AppearancePage)),
			2 => SettingsContentFrame.Navigate(typeof(FoldersPage)),
			3 => SettingsContentFrame.Navigate(typeof(TagsPage)),
			4 => SettingsContentFrame.Navigate(typeof(GitPage)),
			5 => SettingsContentFrame.Navigate(typeof(AdvancedPage)),
			6 => SettingsContentFrame.Navigate(typeof(AboutPage)),
			_ => SettingsContentFrame.Navigate(typeof(AppearancePage))
		};*/
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
