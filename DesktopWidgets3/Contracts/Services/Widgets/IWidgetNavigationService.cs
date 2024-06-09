﻿using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Navigation;

namespace DesktopWidgets3.Contracts.Services.Widgets;

public interface IWidgetNavigationService
{
    event NavigatedEventHandler Navigated;

    bool CanGoBack { get; }

    Frame? Frame { get; set; }

    bool NavigateTo(WidgetType widgetType, object? parameter = null, bool clearNavigation = false);

    bool GoBack();
}
