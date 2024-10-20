﻿using Microsoft.UI.Dispatching;

namespace DesktopWidgets3.Widget;

/// <summary>
/// The widget view model navigation parameter.
/// </summary>
public class WidgetViewModelNavigationParameter
{
    public required string Id { get; set; }

    public required string Type { get; set; }

    public required int Index { get; set; }

    public required DispatcherQueue DispatcherQueue { get; set; }

    public required BaseWidgetSettings Settings { get; set; }
}
