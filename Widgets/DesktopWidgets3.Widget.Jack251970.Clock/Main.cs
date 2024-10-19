using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;

namespace DesktopWidgets3.Widget.Jack251970.Clock;

public partial class Main : IWidgetGroup, IWidgetGroupSetting, IWidgetLocalization, IDisposable
{
    #region IWidgetGroup

    public static WidgetInitContext WidgetInitContext => widgetInitContext;
    private static WidgetInitContext widgetInitContext = null!;

    public void InitWidgetGroup(WidgetInitContext widgetInitContext)
    {
        Main.widgetInitContext = widgetInitContext;
    }

    public FrameworkElement GetWidgetContent(string widgetType, ResourceDictionary? resourceDictionary)
    {
        return widgetType switch
        {
            "SystemInfo_DigitalClock" => new DigitalClockWidget(resourceDictionary),
            "SystemInfo_AnalogClock" => new AnalogClockWidget(resourceDictionary),
            _ => new UserControl()
        };
    }

    #endregion

    #region IWidgetGroupSetting

    public BaseWidgetSettings GetDefaultSettings(string widgetType)
    {
        return widgetType switch
        {
            "SystemInfo_DigitalClock" => new DigitalClockSettings(),
            "SystemInfo_AnalogClock" => new AnalogClockSettings(),
            _ => new BaseWidgetSettings()
        };
    }

    public FrameworkElement GetWidgetSettingContent(string widgetType, ResourceDictionary? resourceDictionary)
    {
        return widgetType switch
        {
            "SystemInfo_DigitalClock" => new DigitalClockSetting(resourceDictionary),
            "SystemInfo_AnalogClock" => new AnalogClockSetting(resourceDictionary),
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
            "SystemInfo_DigitalClock" => WidgetInitContext.LocalizationService.GetLocalizedString("DigitalClock_Name"),
            "SystemInfo_AnalogClock" => WidgetInitContext.LocalizationService.GetLocalizedString("AnalogClock_Name"),
            _ => string.Empty
        };
    }

    public string GetLocalizedWidgetDescription(string widgetType)
    {
        return widgetType switch
        {
            "SystemInfo_DigitalClock" => WidgetInitContext.LocalizationService.GetLocalizedString("DigitalClock_Description"),
            "SystemInfo_AnalogClock" => WidgetInitContext.LocalizationService.GetLocalizedString("AnalogClock_Description"),
            _ => string.Empty
        };
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
