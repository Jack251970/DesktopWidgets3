﻿using CommunityToolkit.Mvvm.ComponentModel;

using Microsoft.UI.Xaml.Navigation;

namespace DesktopWidgets3.ViewModels.Pages;

public partial class NavShellPageViewModel : ObservableRecipient
{
    [ObservableProperty]
    private bool isBackEnabled;

    [ObservableProperty]
    private object? selected;

    public INavigationService NavigationService { get; }

    public INavigationViewService ShellService { get; }

    public NavShellPageViewModel(INavigationService navigationService, INavigationViewService shellService)
    {
        NavigationService = navigationService;
        NavigationService.Navigated += OnNavigated;
        ShellService = shellService;
    }

    private void OnNavigated(object sender, NavigationEventArgs e)
    {
        IsBackEnabled = NavigationService.CanGoBack;

        if (e.SourcePageType == typeof(SettingsPage))
        {
            Selected = ShellService.SettingsItem;
            return;
        }

        var selectedItem = ShellService.GetSelectedItem(e.SourcePageType);
        if (selectedItem != null)
        {
            Selected = selectedItem;
        }
    }
}
