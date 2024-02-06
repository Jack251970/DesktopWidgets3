// Copyright (c) 2023 Files Community
// Licensed under the MIT License. See the LICENSE.

using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Navigation;

namespace Files.App.Views.Settings;

public sealed partial class GitPage : Page
{
	public GitPage()
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
