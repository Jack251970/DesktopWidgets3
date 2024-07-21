// Copyright (c) 2024 Files Community
// Licensed under the MIT License. See the LICENSE.

using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Navigation;

namespace Files.App.Views.Settings;

public sealed partial class DevToolsPage : Page
{
	public DevToolsPage()
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
}
