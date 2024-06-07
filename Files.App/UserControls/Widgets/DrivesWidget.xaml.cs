// Copyright (c) 2024 Files Community
// Licensed under the MIT License. See the LICENSE.

using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Input;

namespace Files.App.UserControls.Widgets;

/// <summary>
/// Represents group of control displays a list of <see cref="WidgetDriveCardItem"/>.
/// </summary>
public sealed partial class DrivesWidget : UserControl
{
    public DrivesWidgetViewModel ViewModel { get; set; } = DependencyExtensions.GetService<DrivesWidgetViewModel>();

    public DrivesWidget(IFolderViewViewModel folderViewViewModel)
    {
        // CHANGE: Initialize view model.
        ViewModel.Initialize(folderViewViewModel);

        InitializeComponent();
    }

    private async void Button_Click(object sender, RoutedEventArgs e)
    {
        if (sender is not Button button ||
            button.Tag.ToString() is not string path)
        {
            return;
        }

        await ViewModel.NavigateToPath(path);
    }

    private async void Button_PointerPressed(object sender, PointerRoutedEventArgs e)
    {
        if (!e.GetCurrentPoint(null).Properties.IsMiddleButtonPressed ||
            sender is not Button button ||
            button.Tag.ToString() is not string path)
        {
            return;
        }

        if (await DriveHelpers.CheckEmptyDrive(ViewModel.FolderViewViewModel, path))
        {
            return;
        }

        await NavigationHelpers.OpenPathInNewTab(ViewModel.FolderViewViewModel, path, false);
    }

    private void Button_RightTapped(object sender, RightTappedRoutedEventArgs e)
    {
        ViewModel.BuildItemContextMenu(sender, e);
    }

    private async void GoToStorageSense_Click(object sender, RoutedEventArgs e)
    {
        if (sender is not Button button ||
            button.Tag.ToString() is not string path)
        {
            return;
        }

        await StorageSenseHelper.OpenStorageSenseAsync(path);
    }
}
