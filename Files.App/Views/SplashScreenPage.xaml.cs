// Copyright (c) 2024 Files Community
// Licensed under the MIT License. See the LICENSE.

using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Navigation;

namespace Files.App.Views;

/// <summary>
/// Display the app splash screen.
/// </summary>
public sealed partial class SplashScreenPage : Page
{
    private IFolderViewViewModel FolderViewViewModel { get; set; } = null!;

#pragma warning disable CA1822 // Mark members as static

    private string BranchLabel =>
        AppLifecycleHelper.AppEnvironment switch
        {
            AppEnvironment.Dev => "Dev",
			AppEnvironment.Preview => "Preview",
			_ => string.Empty,
		};

#pragma warning restore CA1822 // Mark members as static

    public SplashScreenPage()
	{
		InitializeComponent();
	}

    protected override void OnNavigatedTo(NavigationEventArgs e)
    {
        FolderViewViewModel = (IFolderViewViewModel)e.Parameter;

        base.OnNavigatedTo(e);
    }

	private void Image_ImageOpened(object sender, RoutedEventArgs e)
	{
		FolderViewViewModel.SplashScreenLoadingTCS?.TrySetResult();
	}

	private void Image_ImageFailed(object sender, RoutedEventArgs e)
	{
        FolderViewViewModel.SplashScreenLoadingTCS?.TrySetResult();
	}
}
