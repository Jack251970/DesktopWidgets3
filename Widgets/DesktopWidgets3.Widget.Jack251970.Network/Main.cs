using System;
using Microsoft.UI.Xaml;

namespace DesktopWidgets3.Widget.Jack251970.Network;

public class Main : IWidget, IWidgetPin, IWidgetSetting, IWidgetLocalization, IDisposable
{
    #region IWidget

    public static WidgetInitContext Context => context;
    private static WidgetInitContext context = null!;

    private HardwareInfoService HardwareInfoService = null!;

    public void InitWidget(WidgetInitContext context)
    {
        Main.context = context;

        HardwareInfoService = new HardwareInfoService(context);
        HardwareInfoService.StartMonitor(HardwareType.Network);
    }

    public FrameworkElement CreateWidgetFrameworkElement(ResourceDictionary? resourceDictionary)
    {
        return new NetworkWidget(resourceDictionary, HardwareInfoService);
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
        return new NetworkSettings();
    }

    public FrameworkElement CreateWidgetSettingFrameworkElement(ResourceDictionary? resourceDictionary)
    {
        return new NetworkSetting(resourceDictionary);
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
                HardwareInfoService.Dispose();
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
