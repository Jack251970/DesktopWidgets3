using System;
using Microsoft.UI.Xaml;

namespace DesktopWidgets3.Widget.Jack251970.AnalogClock;

public class Main : IWidget, IWidgetEnableDisable, IWidgetSetting, IDisposable
{
    #region IWidget

    private WidgetInitContext Context = null!;

    public void InitWidget(WidgetInitContext context)
    {
        Context = context;
    }

    public FrameworkElement CreateWidgetFrameworkElement()
    {
        return new AnalogClockWidget(Context);
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
        return new AnalogClockSettings();
    }

    public FrameworkElement CreateWidgetSettingFrameworkElement()
    {
        return new AnalogClockSetting(Context);
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
