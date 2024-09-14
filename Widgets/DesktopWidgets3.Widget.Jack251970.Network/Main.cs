using System;
using Microsoft.UI.Xaml;

namespace DesktopWidgets3.Widget.Jack251970.Network;

public class Main : IWidget, IWidgetEnableDisable, IWidgetSetting, IDisposable
{
    #region IWidget

    private WidgetInitContext Context = null!;

    private HardwareInfoService HardwareInfoService = null!;

    public void InitWidget(WidgetInitContext context)
    {
        Context = context;

        HardwareInfoService = new HardwareInfoService(context.API);
        HardwareInfoService.StartMonitor(HardwareType.Network);
    }

    public FrameworkElement CreateWidgetFrameworkElement()
    {
        return new NetworkWidget(Context, HardwareInfoService);
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
        return new NetworkSettings();
    }

    public FrameworkElement CreateWidgetSettingFrameworkElement()
    {
        return new NetworkSetting(Context);
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
