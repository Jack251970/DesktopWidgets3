﻿namespace DesktopWidgets3.Widget.Jack251970.Clock.Views;

internal delegate IWidgetSettingViewBase WidgetSettingCreateDelegate(string widgetId);
internal interface IWidgetSettingViewBase : IDisposable
{
    bool IsNavigated { get; }

    void OnWidgetSettingsChanged(WidgetSettingsChangedArgs contextChangedArgs);
}
