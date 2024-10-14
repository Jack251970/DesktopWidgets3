using Microsoft.UI.Xaml;

namespace DesktopWidgets3.Widget.Jack251970.AnalogClock;

public partial class Main : IWidget, IWidgetPin, IWidgetSetting, IWidgetLocalization, IDisposable
{
    #region IWidget

    public static WidgetInitContext Context => context;
    private static WidgetInitContext context = null!;

    public void InitWidget(WidgetInitContext context)
    {
        Main.context = context;
    }

    public FrameworkElement CreateWidgetFrameworkElement(ResourceDictionary? resourceDictionary)
    {
        return new AnalogClockWidget(resourceDictionary);
    }

    #endregion

    #region IWidgetPin

    public void WidgetPinning(bool firstWidget)
    {

    }

    public void WidgetUnpinned(bool lastWidget)
    {

    }

    #endregion

    #region IWidgetSetting

    public BaseWidgetSettings GetDefaultSetting()
    {
        return new AnalogClockSettings();
    }

    public FrameworkElement CreateWidgetSettingFrameworkElement(ResourceDictionary? resourceDictionary)
    {
        return new AnalogClockSetting(resourceDictionary);
    }

    #endregion

    #region IWidgetLocalization

    public string GetLocalizatedTitle()
    {
        return Context.LocalizationService.GetLocalizedString("Title");
    }

    public string GetLocalizatedDescription()
    {
        return Context.LocalizationService.GetLocalizedString("Description");
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
