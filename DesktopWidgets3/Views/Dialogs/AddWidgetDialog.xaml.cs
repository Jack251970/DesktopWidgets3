// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using CommunityToolkit.Mvvm.Input;
using Microsoft.UI.Dispatching;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Automation;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Media;
using Microsoft.UI.Xaml.Shapes;
using Microsoft.Windows.Widgets.Hosts;
using Serilog;

namespace DesktopWidgets3.Views.Dialogs;

public sealed partial class AddWidgetDialog : ContentDialog
{
    private static readonly ILogger _log = Log.ForContext("SourceContext", nameof(AddWidgetDialog));

    private object _selectedWidget = null!;

    public object AddedWidget { get; private set; } = null!;

    public AddWidgetViewModel ViewModel { get; set; }

    private readonly DispatcherQueue _dispatcherQueue;
    private readonly MicrosoftWidgetModel _microsoftWidgetModel;

    private readonly IAppSettingsService _appSettingsService;
    private readonly IWidgetResourceService _widgetResourceService;

    public AddWidgetDialog()
    {
        ViewModel = DependencyExtensions.GetRequiredService<AddWidgetViewModel>();

        _dispatcherQueue = DependencyExtensions.GetRequiredService<DispatcherQueue>();
        _microsoftWidgetModel = DependencyExtensions.GetRequiredService<MicrosoftWidgetModel>();

        _appSettingsService = DependencyExtensions.GetRequiredService<AppSettingsService>();
        _widgetResourceService = DependencyExtensions.GetRequiredService<IWidgetResourceService>();

        InitializeComponent();

        RequestedTheme = DependencyExtensions.GetRequiredService<IThemeSelectorService>().Theme;
    }

    private void ContentDialog_SizeChanged(object? sender, SizeChangedEventArgs e)
    {
        var contentDialogMaxHeight = (double)Resources["ContentDialogMaxHeight"];
        const int SmallThreshold = 324;
        const int MediumThreshold = 360;

        var smallPinButtonMargin = (Thickness)Resources["SmallPinButtonMargin"];
        var largePinButtonMargin = (Thickness)Resources["LargePinButtonMargin"];
        var smallWidgetPreviewTopMargin = (Thickness)Resources["SmallWidgetPreviewTopMargin"];
        var largeWidgetPreviewTopMargin = (Thickness)Resources["LargeWidgetPreviewTopMargin"];

        AddWidgetNavigationView.Height = Math.Min(ActualHeight, contentDialogMaxHeight) - AddWidgetTitleBar.ActualHeight;

        var previewHeightAvailable = AddWidgetNavigationView.Height - TitleRow.ActualHeight - PinRow.ActualHeight;

        // Adjust margins when the height gets too small to show everything.
        if (previewHeightAvailable < SmallThreshold)
        {
            PreviewRow.Padding = smallWidgetPreviewTopMargin;
            PinButton.Margin = smallPinButtonMargin;
        }
        else if (previewHeightAvailable < MediumThreshold)
        {
            PreviewRow.Padding = smallWidgetPreviewTopMargin;
            PinButton.Margin = largePinButtonMargin;
        }
        else
        {
            PreviewRow.Padding = largeWidgetPreviewTopMargin;
            PinButton.Margin = largePinButtonMargin;
        }
    }

    #region Load Widgets

    [RelayCommand]
    public async Task OnLoadedAsync()
    {
        AddWidgetNavigationView.MenuItems.Clear();

        // load the desktop widget 3 widgets
        await FillAvailableDesktopWidget3WidgetsAsync();

        // load the microsoft widgets
        await FillAvailableMicrosoftWidgetsAsync();

        // select the first widget by default
        SelectFirstWidgetByDefault();

        // bind the microsoft widgets event
        _microsoftWidgetModel.WidgetDefinitionDeleted += WidgetCatalog_WidgetDefinitionDeleted;
    }

    #region Desktop Widgets 3 Widgets

    private async Task FillAvailableDesktopWidget3WidgetsAsync()
    {
        // Show the widget group and widgets underneath them in alphabetical order.
        var installedWidgetGroups = _widgetResourceService.GetInstalledDashboardGroupItems().OrderBy(x => x.Name);
        var currentlyPinnedWidgets = _appSettingsService.GetWidgetsList();

        foreach (var widgetGroup in installedWidgetGroups)
        {
            var navItem = new NavigationViewItem
            {
                IsExpanded = true,
                Tag = widgetGroup,
                Content = new TextBlock { Text = widgetGroup.Name, TextWrapping = TextWrapping.Wrap },
            };

            navItem.SetValue(ToolTipService.ToolTipProperty, widgetGroup.Name);

            foreach (var widgetType in widgetGroup.Types)
            {
                var widgetId = widgetGroup.Id;
                var widgetName = _widgetResourceService.GetWidgetName(WidgetProviderType.DesktopWidgets3, widgetId, widgetType);
                var widgetDefinition = new DesktopWidgets3WidgetDefinition(widgetId, widgetType, widgetGroup.Name, widgetName);

                var subItemContent = await BuildWidgetNavItemAsync(widgetDefinition);
                var enable = !_widgetResourceService.IsWidgetSingleInstanceAndAlreadyPinned(widgetId, widgetType, currentlyPinnedWidgets);
                var subItem = new NavigationViewItem
                {
                    Tag = widgetDefinition,
                    Content = subItemContent,
                    IsEnabled = enable,
                };
                subItem.SetValue(AutomationProperties.AutomationIdProperty, $"NavViewItem_{widgetId}_{widgetType}");
                subItem.SetValue(AutomationProperties.NameProperty, widgetName);
                subItem.SetValue(ToolTipService.ToolTipProperty, widgetName);

                navItem.MenuItems.Add(subItem);
            }

            if (navItem.MenuItems.Count > 0)
            {
                AddWidgetNavigationView.MenuItems.Add(navItem);
            }
        }
    }

    private async Task<Grid> BuildWidgetNavItemAsync(DesktopWidgets3WidgetDefinition widgetDefinition)
    {
        return await BuildNavItemAsync(widgetDefinition);
    }

    private async Task<Grid> BuildNavItemAsync(DesktopWidgets3WidgetDefinition widgetDefinition)
    {
        var imageBrush = await _widgetResourceService.GetWidgetIconBrushAsync(_dispatcherQueue, WidgetProviderType.DesktopWidgets3, widgetDefinition.WidgetId, widgetDefinition.WidgetType, ActualTheme);

        return BuildNavItem(imageBrush, widgetDefinition.DisplayTitle);
    }

    #endregion

    #region Microsoft Widgets

    private async Task FillAvailableMicrosoftWidgetsAsync()
    {
        // Fill NavigationView Menu with Widget Providers, and group widgets under each provider.
        // Tag each item with the widget or provider definition, so that it can be used to create
        // the widget if it is selected later.
        var currentlyPinnedWidgets = await _microsoftWidgetModel.GetComSafeWidgetsAsync();

        foreach (var providerDef in _microsoftWidgetModel.WidgetProviderDefinitions)
        {
            if (providerDef.DisplayName == "PeregrineWidgets")
            {
                continue;  // CHANGE: PeregrineWidgets can cause issues in IsSingleInstanceAndAlreadyPinned function
            }

            if (await WidgetHelpers.IsIncludedWidgetProviderAsync(providerDef))
            {
                var navItem = new NavigationViewItem
                {
                    IsExpanded = true,
                    Tag = providerDef,
                    Content = new TextBlock { Text = providerDef.DisplayName, TextWrapping = TextWrapping.Wrap },
                };

                navItem.SetValue(ToolTipService.ToolTipProperty, providerDef.DisplayName);

                foreach (var widgetDef in _microsoftWidgetModel.WidgetDefinitions)
                {
                    if (widgetDef.ProviderDefinitionId.Equals(providerDef.Id, StringComparison.Ordinal))
                    {
                        var subItemContent = await BuildWidgetNavItemAsync(widgetDef);
                        var enable = !_widgetResourceService.IsWidgetSingleInstanceAndAlreadyPinned(widgetDef, currentlyPinnedWidgets);
                        var subItem = new NavigationViewItem
                        {
                            Tag = widgetDef,
                            Content = subItemContent,
                            IsEnabled = enable,
                        };
                        subItem.SetValue(AutomationProperties.AutomationIdProperty, $"NavViewItem_{widgetDef.Id}");
                        subItem.SetValue(AutomationProperties.NameProperty, widgetDef.DisplayTitle);
                        subItem.SetValue(ToolTipService.ToolTipProperty, widgetDef.DisplayTitle);

                        navItem.MenuItems.Add(subItem);
                    }
                }

                if (navItem.MenuItems.Count > 0)
                {
                    AddWidgetNavigationView.MenuItems.Add(navItem);
                }
            }
        }

        /*// If there were no available widgets, log an error.
        // This should never happen since Dev Home's core widgets are always available.
        if (!AddWidgetNavigationView.MenuItems.Any())
        {
            _log.Error($"FillAvailableWidgetsAsync found no available widgets.");
        }*/
    }

    private async Task<Grid> BuildWidgetNavItemAsync(ComSafeWidgetDefinition widgetDefinition)
    {
        return await BuildNavItemAsync(widgetDefinition);
    }

    private async Task<Grid> BuildNavItemAsync(ComSafeWidgetDefinition widgetDefinition)
    {
        var imageBrush = await _widgetResourceService.GetWidgetIconBrushAsync(_dispatcherQueue, widgetDefinition, ActualTheme);

        return BuildNavItem(imageBrush, widgetDefinition.DisplayTitle);
    }

    #endregion

    private static Grid BuildNavItem(Brush widgetImageBrush, string widgetName)
    {
        var itemContent = new Grid
        {
            ColumnDefinitions =
            {
                new ColumnDefinition { Width = GridLength.Auto },
                new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) },
            },
        };

        var itemSquare = new Rectangle()
        {
            Width = 16,
            Height = 16,
            Margin = new Thickness(0, 0, 8, 0),
            Fill = widgetImageBrush,
        };

        Grid.SetColumn(itemSquare, 0);
        itemContent.Children.Add(itemSquare);

        var itemText = new TextBlock()
        {
            Text = widgetName,
            TextWrapping = TextWrapping.Wrap,
            VerticalAlignment = VerticalAlignment.Center,
        };
        Grid.SetColumn(itemText, 1);
        itemContent.Children.Add(itemText);

        return itemContent;
    }

    // TODO: Fix selected index jump issue when loading is very slow and user clicks on a widget before it is loaded.
    private void SelectFirstWidgetByDefault()
    {
        if (AddWidgetNavigationView.MenuItems.Count > 0)
        {
            var firstProvider = AddWidgetNavigationView.MenuItems[0] as NavigationViewItem;
            if (firstProvider?.MenuItems.Count > 0)
            {
                var firstWidget = firstProvider.MenuItems[0] as NavigationViewItem;
                AddWidgetNavigationView.SelectedItem = firstWidget;
            }
        }
    }

    #endregion

    #region Select Widget

    private async void AddWidgetNavigationView_SelectionChanged(
        NavigationView sender,
        NavigationViewSelectionChangedEventArgs _)
    {
        // Selected item could be null if list of widgets became empty, but list should never be empty
        // since core widgets are always available.
        if (sender.SelectedItem is null)
        {
            ViewModel.Clear();
            return;
        }

        // Get selected widget definition.
        var selectedTag = (sender.SelectedItem as NavigationViewItem)?.Tag;
        if (selectedTag is null)
        {
            _log.Error($"Selected widget description did not have a tag");
            ViewModel.Clear();
            return;
        }

        // If the user has selected a widget, show preview. If they selected a provider, leave space blank.
        if (selectedTag as ComSafeWidgetDefinition is ComSafeWidgetDefinition selectedWidgetDefinition)
        {
            _selectedWidget = selectedWidgetDefinition;
            await ViewModel.SetWidgetDefinition(selectedWidgetDefinition, ActualTheme);
        }
        else if (selectedTag as DesktopWidgets3WidgetDefinition is DesktopWidgets3WidgetDefinition selectedWidgetDefinition1)
        {
            // TODO: Fix ViewModel == null issue.
            _selectedWidget = selectedWidgetDefinition1;
            await ViewModel.SetWidgetDefinition(selectedWidgetDefinition1, ActualTheme);
        }
        else if (selectedTag as WidgetProviderDefinition is not null)
        {
            ViewModel.Clear();
        }
    }

    [RelayCommand]
    private void PinButtonClick()
    {
        _log.Debug($"Pin selected");
        AddedWidget = _selectedWidget;

        HideDialog();
    }

    #endregion

    #region Hide Dialog

    [RelayCommand]
    private void CancelButtonClick()
    {
        _log.Debug($"Canceled dialog");
        AddedWidget = null!;

        HideDialog();
    }

    private void HideDialog()
    {
        _selectedWidget = null!;
        ViewModel = null!;

        _microsoftWidgetModel.WidgetDefinitionDeleted -= WidgetCatalog_WidgetDefinitionDeleted;

        Hide();
    }

    #endregion

    // TODO: Support WidgetCatalog_WidgetDefinitionAdded and Updated?

    private void WidgetCatalog_WidgetDefinitionDeleted(WidgetCatalog sender, WidgetDefinitionDeletedEventArgs args)
    {
        var deletedDefinitionId = args.DefinitionId;

        _dispatcherQueue.TryEnqueue(() =>
        {
            // If we currently have the deleted widget open, un-select it.
            if (_selectedWidget is not null &&
                _selectedWidget as ComSafeWidgetDefinition is ComSafeWidgetDefinition selectedWidgetDefinition &&
                selectedWidgetDefinition.Id.Equals(deletedDefinitionId, StringComparison.Ordinal))
            {
                _log.Information($"Widget definition deleted while selected.");
                ViewModel.Clear();
                AddWidgetNavigationView.SelectedItem = null;
            }

            // Remove the deleted WidgetDefinition from the list of available widgets.
            var menuItems = AddWidgetNavigationView.MenuItems;
            foreach (var providerItem in menuItems.OfType<NavigationViewItem>())
            {
                foreach (var widgetItem in providerItem.MenuItems.OfType<NavigationViewItem>())
                {
                    if (widgetItem.Tag is ComSafeWidgetDefinition tagDefinition)
                    {
                        if (tagDefinition.Id.Equals(deletedDefinitionId, StringComparison.Ordinal))
                        {
                            providerItem.MenuItems.Remove(widgetItem);

                            // If we've removed all widgets from a provider, remove the provider from the list.
                            if (!providerItem.MenuItems.Any())
                            {
                                menuItems.Remove(providerItem);

                                // If we've removed all providers from the list, log an error.
                                // This should never happen since Dev Home's core widgets are always available.
                                if (!menuItems.Any())
                                {
                                    _log.Error($"WidgetCatalog_WidgetDefinitionDeleted found no available widgets.");
                                }
                            }

                            return;
                        }
                    }
                }
            }
        });
    }

    #region Update Theme

    [RelayCommand]
    private async Task UpdateThemeAsync()
    {
        // Update the icon and screenshot for the selected widget.
        await ViewModel.UpdateThemeAsync(ActualTheme);

        // Update the icons for each available widget listed.
        foreach (var providerItem in AddWidgetNavigationView.MenuItems.OfType<NavigationViewItem>())
        {
            foreach (var widgetItem in providerItem.MenuItems.OfType<NavigationViewItem>())
            {
                if (widgetItem.Tag as ComSafeWidgetDefinition is ComSafeWidgetDefinition widgetDefinition)
                {
                    widgetItem.Content = await BuildNavItemAsync(widgetDefinition);
                }
                else if (widgetItem.Tag as DesktopWidgets3WidgetDefinition is DesktopWidgets3WidgetDefinition widgetDefinition1)
                {
                    widgetItem.Content = await BuildNavItemAsync(widgetDefinition1);
                }
            }
        }
    }

    #endregion
}
