using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;

namespace DesktopWidgets3.Widget.Jack251970.SystemInfo;

public partial class Main : IWidgetGroup, IWidgetGroupSetting, IWidgetLocalization, IDisposable
{
    #region IWidgetGroup

    public static IWidgetInitContext WidgetInitContext => widgetInitContext;
    private static IWidgetInitContext widgetInitContext = null!;

    private HardwareInfoService HardwareInfoService = new();

    public void InitWidgetGroup(IWidgetInitContext widgetInitContext)
    {
        Main.widgetInitContext = widgetInitContext;

        WidgetInitContext.SettingsService.OnBatterySaverChanged += OnBatterySaverChanged;

        HardwareInfoService.SampleTimer.Interval = widgetInitContext.SettingsService.BatterySaver ? 1000 : 200;
        HardwareInfoService.StartMonitor(HardwareType.CPU);
        HardwareInfoService.StartMonitor(HardwareType.GPU);
        HardwareInfoService.StartMonitor(HardwareType.Memory);
        HardwareInfoService.StartMonitor(HardwareType.Network);
        HardwareInfoService.StartMonitor(HardwareType.Disk);

        HardwareInfoService.SampleTimer.Enabled = true;
    }

    public FrameworkElement CreateWidgetContent(IWidgetContext widgetContext, ResourceDictionary? resourceDictionary)
    {
        return widgetContext.Type switch
        {
            "SystemInfo_Performance" => new PerformanceWidget(resourceDictionary, HardwareInfoService),
            "SystemInfo_Network" => new NetworkWidget(resourceDictionary, HardwareInfoService),
            "SystemInfo_Disk" => new DiskWidget(resourceDictionary, HardwareInfoService),
            _ => new UserControl()
        };
    }

    public void DeleteWidget(string widgetId, BaseWidgetSettings widgetSettings)
    {

    }

    public void ActivateWidget(IWidgetContext widgetContext)
    {

    }

    public void DeactivateWidget(string widgetId)
    {

    }

    #endregion

    #region IWidgetGroupSetting

    public BaseWidgetSettings GetDefaultSettings(string widgetType)
    {
        return widgetType switch
        {
            "SystemInfo_Performance" => new PerformanceSettings(),
            "SystemInfo_Network" => new NetworkSettings(),
            "SystemInfo_Disk" => new DiskSettings(),
            _ => new BaseWidgetSettings()
        };
    }

    public FrameworkElement CreateWidgetSettingContent(string widgetType, ResourceDictionary? resourceDictionary)
    {
        return widgetType switch
        {
            "SystemInfo_Performance" => new PerformanceSetting(resourceDictionary),
            "SystemInfo_Network" => new NetworkSetting(resourceDictionary),
            "SystemInfo_Disk" => new DiskSetting(resourceDictionary),
            _ => new UserControl()
        };
    }

    #endregion

    #region IWidgetLocalization

    public string GetLocalizedWidgetGroupName()
    {
        return WidgetInitContext.LocalizationService.GetLocalizedString("Name");
    }

    public string GetLocalizedWidgetGroupDescription()
    {
        return WidgetInitContext.LocalizationService.GetLocalizedString("Description");
    }

    public string GetLocalizedWidgetName(string widgetType)
    {
        return widgetType switch
        {
            "SystemInfo_Performance" => WidgetInitContext.LocalizationService.GetLocalizedString("Performance_Name"),
            "SystemInfo_Network" => WidgetInitContext.LocalizationService.GetLocalizedString("Network_Name"),
            "SystemInfo_Disk" => WidgetInitContext.LocalizationService.GetLocalizedString("Disk_Name"),
            _ => string.Empty
        };
    }

    public string GetLocalizedWidgetDescription(string widgetType)
    {
        return widgetType switch
        {
            "SystemInfo_Performance" => WidgetInitContext.LocalizationService.GetLocalizedString("Performance_Description"),
            "SystemInfo_Network" => WidgetInitContext.LocalizationService.GetLocalizedString("Network_Description"),
            "SystemInfo_Disk" => WidgetInitContext.LocalizationService.GetLocalizedString("Disk_Description"),
            _ => string.Empty
        };
    }

    #endregion

    #region Battery Saver

    private void OnBatterySaverChanged(bool enable)
    {
        var enabled = HardwareInfoService.SampleTimer.Enabled;
        HardwareInfoService.SampleTimer.Enabled = false;
        HardwareInfoService.SampleTimer.Interval = enable ? 1000 : 200;
        HardwareInfoService.SampleTimer.Enabled = enabled;
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
                WidgetInitContext.SettingsService.OnBatterySaverChanged -= OnBatterySaverChanged;
                HardwareInfoService.Dispose();
                HardwareInfoService = null!;
                widgetInitContext = null!;
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
