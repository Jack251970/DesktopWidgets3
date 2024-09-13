using System;
using DesktopWidgets3.Widget.Jack251970.Disk.Setting;
using DesktopWidgets3.Widget.Jack251970.Disk.Views;
using Microsoft.UI.Xaml;

namespace DesktopWidgets3.Widget.Jack251970.Disk;

public class Main : IWidget, IWidgetEnableDisable, IWidgetSetting, IDisposable
{
    #region IWidget

    private WidgetInitContext Context = null!;

    public FrameworkElement CreateWidgetFrameworkElement()
    {
        return new ClockWidget();
    }

    public void InitWidget(WidgetInitContext context)
    {
        Context = context;
    }

    public void EnableWidget(bool firstWidget)
    {

    }

    public void DisableWidget(bool lastWidget)
    {

    }

    #endregion

    #region IWidgetSetting

    public BaseWidgetSettings GetDefaultSetting()
    {
        return new DigitalClockSettings();
    }

    public FrameworkElement CreateWidgetSettingFrameworkElement()
    {
        return new DigitalClockSetting(Context);
    }

    #endregion

    #region IDisposable

    private bool disposed;

    protected virtual void Dispose(bool disposing)
    {
        if (!disposed)
        {
            if (disposing)
            {
                Context = null!;
            }

            disposed = true;
        }
    }

    public void Dispose()
    {
        Dispose(true);
        GC.SuppressFinalize(this);
    }

    #endregion
}
