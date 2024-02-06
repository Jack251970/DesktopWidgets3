// Copyright (c) 2023 Files Community
// Licensed under the MIT License. See the LICENSE.

using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Navigation;

namespace Files.App.Views.Settings;

public sealed partial class AdvancedPage : Page
{
	public AdvancedPage()
	{
		InitializeComponent();
	}

    protected override void OnNavigatedTo(NavigationEventArgs e)
    {
        // CHANGE: Initialize folder view view model and related services.
        if (e.Parameter is IFolderViewViewModel folderViewViewModel)
        {
            ViewModel.Initialize(folderViewViewModel);
        }
    }
}
