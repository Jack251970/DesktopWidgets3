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

    private bool _isHidden = false;

    public AddWidgetDialog()
    {
        ViewModel = DependencyExtensions.GetRequiredService<AddWidgetViewModel>();

        _dispatcherQueue = DependencyExtensions.GetRequiredService<DispatcherQueue>();
        _microsoftWidgetModel = DependencyExtensions.GetRequiredService<MicrosoftWidgetModel>();

        _appSettingsService = DependencyExtensions.GetRequiredService<IAppSettingsService>();
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
        if (!_isHidden)
        {
            AddWidgetNavigationView.SelectionChanged += AddWidgetNavigationView_SelectionChanged;
            SelectFirstWidgetByDefault();
        }

        // bind the microsoft widgets event
        if (!_isHidden)
        {
            _microsoftWidgetModel.WidgetDefinitionDeleted += WidgetCatalog_WidgetDefinitionDeleted;
        }
    }

    #region Desktop Widgets 3 Widgets

    private async Task FillAvailableDesktopWidget3WidgetsAsync()
    {
        // Show the widget group and widgets underneath them in alphabetical order.
        var installedWidgetGroups = (await _widgetResourceService.GetInstalledDashboardGroupItems()).OrderBy(x => x.Name);
        var currentlyPinnedWidgets = _appSettingsService.GetWidgetsList();

        foreach (var widgetGroup in installedWidgetGroups)
        {
            if (_isHidden)
            {
                return;
            }

            var itemContent = BuildWidgetGroupNavItem(widgetGroup);
            var navItem = new NavigationViewItem
            {
                IsExpanded = true,
                Tag = widgetGroup,
                Content = itemContent,
            };

            navItem.SetValue(ToolTipService.ToolTipProperty, widgetGroup.Name);

            foreach (var widgetType in widgetGroup.Types)
            {
                if (_isHidden)
                {
                    return;
                }

                var widgetId = widgetGroup.Id;
                var widgetName = _widgetResourceService.GetWidgetName(WidgetProviderType.DesktopWidgets3, widgetId, widgetType);
                var widgetDefinition = new DesktopWidgets3WidgetDefinition(widgetId, widgetType, widgetGroup.Name, widgetName);

                var subItemContent = await BuildWidgetNavItemAsync(widgetDefinition);
                var enable = !IsWidgetSingleInstanceAndAlreadyPinned(widgetId, widgetType, currentlyPinnedWidgets);
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

    private static Grid BuildWidgetGroupNavItem(DashboardWidgetGroupItem widgetGroupItem)
    {
        var imageBrush = widgetGroupItem.IconFill;

        return BuildNavItem(imageBrush, widgetGroupItem.Name);
    }

    private async Task<Grid> BuildWidgetNavItemAsync(DesktopWidgets3WidgetDefinition widgetDefinition)
    {
        var imageBrush = await _widgetResourceService.GetWidgetIconBrushAsync(WidgetProviderType.DesktopWidgets3, widgetDefinition.WidgetId, widgetDefinition.WidgetType, ActualTheme);

        return BuildNavItem(imageBrush, widgetDefinition.DisplayTitle);
    }

    private bool IsWidgetSingleInstanceAndAlreadyPinned(string widgetId, string widgetType, List<JsonWidgetItem> currentlyPinnedWidgets)
    {
        if (!_widgetResourceService.GetWidgetAllowMultiple(WidgetProviderType.DesktopWidgets3, widgetId, widgetType))
        {
            foreach (var pinnedWidget in currentlyPinnedWidgets)
            {
                if (pinnedWidget.Equals(WidgetProviderType.DesktopWidgets3, widgetId, widgetType))
                {
                    return true;
                }
            }
        }

        return false;
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
            if (_isHidden)
            {
                return;
            }

            if (WidgetHelpers.IsIncludedWidgetProvider(providerDef))
            {
                var itemContent = await BuildWidgetGroupNavItemAsync(providerDef);
                var navItem = new NavigationViewItem
                {
                    IsExpanded = true,
                    Tag = providerDef,
                    Content = itemContent,
                };

                navItem.SetValue(ToolTipService.ToolTipProperty, providerDef.DisplayName);

                foreach (var widgetDef in _microsoftWidgetModel.WidgetDefinitions)
                {
                    if (_isHidden)
                    {
                        return;
                    }

                    if (widgetDef.ProviderDefinitionId.Equals(providerDef.Id, StringComparison.Ordinal))
                    {
                        var subItemContent = await BuildWidgetNavItemAsync(widgetDef);
                        var enable = !IsWidgetSingleInstanceAndAlreadyPinned(widgetDef, currentlyPinnedWidgets);
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

    private async Task<Grid> BuildWidgetGroupNavItemAsync(WidgetProviderDefinition widgetProviderDefinition)
    {
        var imageBrush = await _widgetResourceService.GetWidgetGroupIconBrushAsync(widgetProviderDefinition);

        return BuildNavItem(imageBrush, widgetProviderDefinition.DisplayName);
    }

    private async Task<Grid> BuildWidgetNavItemAsync(ComSafeWidgetDefinition widgetDefinition)
    {
        var imageBrush = await _widgetResourceService.GetWidgetIconBrushAsync(widgetDefinition, ActualTheme);

        return BuildNavItem(imageBrush, widgetDefinition.DisplayTitle);
    }

    private static bool IsWidgetSingleInstanceAndAlreadyPinned(ComSafeWidgetDefinition widgetDef, ComSafeWidget[]? currentlyPinnedWidgets)
    {
        // If a WidgetDefinition has AllowMultiple = false, only one of that widget can be pinned at one time.
        if (!widgetDef.AllowMultiple)
        {
            if (currentlyPinnedWidgets != null)
            {
                foreach (var pinnedWidget in currentlyPinnedWidgets)
                {
                    if (pinnedWidget.DefinitionId == widgetDef.Id)
                    {
                        return true;
                    }
                }
            }
        }

        return false;
    }

    #endregion

    private static Grid BuildNavItem(Brush? widgetImageBrush, string widgetName)
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

    private void SelectFirstWidgetByDefault()
    {
        // If the view model is already valid, don't select a default widget.
        // Because the user may have already selected a widget before the widgets were all loaded.
        if (ViewModel != null && ViewModel.IsValid())
        {
            return;
        }

        var navViewItemsCount = AddWidgetNavigationView.MenuItems.Count;
        if (navViewItemsCount > 0)
        {
            for (var i = 0; i < navViewItemsCount; i++)
            {
                var provider = AddWidgetNavigationView.MenuItems[i] as NavigationViewItem;
                var providerItemsCount = provider?.MenuItems.Count ?? 0;
                if (providerItemsCount > 0)
                {
                    for (var j = 0; j < providerItemsCount; j++)
                    {
                        var widget = provider!.MenuItems[j] as NavigationViewItem;
                        if (widget != null && widget.IsEnabled)
                        {
                            AddWidgetNavigationView.SelectedItem = widget;
                            return;
                        }
                    }
                }
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
        _isHidden = true;
        _selectedWidget = null!;
        ViewModel = null!;

        AddWidgetNavigationView.SelectionChanged -= AddWidgetNavigationView_SelectionChanged;
        _microsoftWidgetModel.WidgetDefinitionDeleted -= WidgetCatalog_WidgetDefinitionDeleted;

        Hide();
    }

    #endregion

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
                    widgetItem.Content = await BuildWidgetNavItemAsync(widgetDefinition);
                }
                else if (widgetItem.Tag as DesktopWidgets3WidgetDefinition is DesktopWidgets3WidgetDefinition widgetDefinition1)
                {
                    widgetItem.Content = await BuildWidgetNavItemAsync(widgetDefinition1);
                }
            }
        }
    }

    #endregion
}
