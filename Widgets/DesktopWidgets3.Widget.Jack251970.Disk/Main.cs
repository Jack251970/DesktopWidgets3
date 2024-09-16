using System;
using Microsoft.UI.Xaml;

namespace DesktopWidgets3.Widget.Jack251970.Disk;

public class Main : IWidget, IWidgetEnableDisable, IWidgetSetting, IWidgetLocalization, IDisposable
{
    #region IWidget

    public static WidgetInitContext Context => context;
    private static WidgetInitContext context = null!;

    private HardwareInfoService HardwareInfoService = null!;

    public void InitWidget(WidgetInitContext context)
    {
        Main.context = context;

        HardwareInfoService = new HardwareInfoService(context);
        HardwareInfoService.StartMonitor(HardwareType.Disk);
    }

    public FrameworkElement CreateWidgetFrameworkElement(ResourceDictionary? resourceDictionary)
    {
        return new DiskWidget(resourceDictionary, HardwareInfoService);
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
        return new DiskSettings();
    }

    public FrameworkElement CreateWidgetSettingFrameworkElement(ResourceDictionary? resourceDictionary)
    {
        return new DiskSetting(resourceDictionary);
    }

    #endregion

    #region IWidgetLocalization

    public string GetLocalizatedTitle()
    {
        return Context.LocalizationService.GetLocalizedString("DekstopWidgets3_Widget_Disk_Title");
    }

    public string GetLocalizatedDescription()
    {
        return Context.LocalizationService.GetLocalizedString("DekstopWidgets3_Widget_Disk_Description");
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
