﻿using Microsoft.UI.Xaml;

namespace DesktopWidgets3.Widget.Contracts.Main;

public interface IAsyncWidget
{
    FrameworkElement CreateWidgetFrameworkElement();

    Task InitWidgetClassAsync(WidgetInitContext context);

    Task InitWidgetInstanceAsync(WidgetInitContext context);
}
