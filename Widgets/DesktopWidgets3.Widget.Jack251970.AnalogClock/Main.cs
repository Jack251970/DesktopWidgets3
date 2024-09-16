using System;
using Microsoft.UI.Xaml;

namespace DesktopWidgets3.Widget.Jack251970.AnalogClock;

public class Main : IWidget, IWidgetEnableDisable, IWidgetSetting, IWidgetLocalization, IDisposable
{
    #region IWidget

    public static WidgetInitContext Context => context;
    private static WidgetInitContext context = null!;

    public void InitWidget(WidgetInitContext context)
    {
        Main.context = context;
    }

    public FrameworkElement CreateWidgetFrameworkElement()
    {
        return new AnalogClockWidget();
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
        return new AnalogClockSetting();
    }

    #endregion

    #region IWidgetLocalization

    public string GetLocalizatedTitle()
    {
        return Context.LocalizationService.GetLocalizedString("DekstopWidgets3_Widget_AnalogClock_Title");
    }

    public string GetLocalizatedDescription()
    {
        return Context.LocalizationService.GetLocalizedString("DekstopWidgets3_Widget_AnalogClock_Description");
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
                context = null!;
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
