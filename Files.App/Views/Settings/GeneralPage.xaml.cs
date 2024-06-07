// Copyright (c) 2024 Files Community
// Licensed under the MIT License. See the LICENSE.

using Files.App.ViewModels.Settings;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Navigation;

namespace Files.App.Views.Settings;

public sealed partial class GeneralPage : Page
{
	public GeneralPage()
	{
		InitializeComponent();
	}

    protected override void OnNavigatedTo(NavigationEventArgs e)
    {
        // CHANGE: Initialize folder view view model.
        if (e.Parameter is IFolderViewViewModel folderViewViewModel)
        {
            ViewModel.Initialize(folderViewViewModel);
        }
    }

    private void RemoveStartupPage(object sender, RoutedEventArgs e)
	{
		ViewModel.RemovePageCommand.Execute((sender as FrameworkElement)!.DataContext as PageOnStartupViewModel);
	}
}
