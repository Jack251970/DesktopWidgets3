using System.Collections.Concurrent;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;

namespace DesktopWidgets3.Widget.Jack251970.SystemInfo;

public partial class Main : IWidgetGroup, IWidgetGroupSetting, IWidgetLocalization, IDisposable
{
    public static IWidgetInitContext WidgetInitContext => widgetInitContext;
    private static IWidgetInitContext widgetInitContext = null!;

    private HardwareInfoService HardwareInfoService = new();

    private static readonly Dictionary<string, WidgetCreateDelegate> _widgetTypeRegistry = [];

    private readonly ConcurrentDictionary<string, IWidgetViewBase> _runningWidgets = [];

    private static readonly Dictionary<string, WidgetSettingCreateDelegate> _widgetSettingTypeRegistry = [];

    private readonly ConcurrentDictionary<string, IWidgetSettingViewBase> _runningWidgetSettings = [];

    #region Constructor

    public Main()
    {
        // Register widgets
        _widgetTypeRegistry.Add(PerformanceWidget.Type, (widgetId, resourceDictionary, hardwareInfoService) => new PerformanceWidget(widgetId, resourceDictionary, hardwareInfoService));
        _widgetTypeRegistry.Add(NetworkWidget.Type, (widgetId, resourceDictionary, hardwareInfoService) => new NetworkWidget(widgetId, resourceDictionary, hardwareInfoService));
        _widgetTypeRegistry.Add(DiskWidget.Type, (widgetId, resourceDictionary, hardwareInfoService) => new DiskWidget(widgetId, resourceDictionary, hardwareInfoService));

        // Register widget settings
        _widgetSettingTypeRegistry.Add(PerformanceWidget.Type, (widgetId, resourceDictionary) => new PerformanceSetting(widgetId, resourceDictionary));
        _widgetSettingTypeRegistry.Add(NetworkWidget.Type, (widgetId, resourceDictionary) => new NetworkSetting(widgetId, resourceDictionary));
        _widgetSettingTypeRegistry.Add(DiskWidget.Type, (widgetId, resourceDictionary) => new DiskSetting(widgetId, resourceDictionary));
    }

    #endregion

    #region IWidgetGroup

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
        var widgetId = widgetContext.Id;
        var widgetType = widgetContext.Type;

        if (!_widgetTypeRegistry.TryGetValue(widgetType, out var value))
        {
            return new UserControl();
        }

        if (_runningWidgets.TryGetValue(widgetId, out var widget))
        {
            return (FrameworkElement)widget;
        }

        var factory = value;
        var widgetView = factory(widgetId, resourceDictionary, HardwareInfoService);
        _runningWidgets.TryAdd(widgetId, widgetView);

        return (FrameworkElement)widgetView;
    }

    public void UnpinWidget(string widgetId, BaseWidgetSettings widgetSettings)
    {
        if (_runningWidgets.TryGetValue(widgetId, out var widget))
        {
            widget.Dispose();
            _runningWidgets.TryRemove(widgetId, out _);
        }
    }

    public void DeleteWidget(string widgetId, BaseWidgetSettings widgetSettings)
    {
        if (_runningWidgets.TryGetValue(widgetId, out var widget))
        {
            widget.Dispose();
            _runningWidgets.TryRemove(widgetId, out _);
        }
    }

    public void ActivateWidget(IWidgetContext widgetContext)
    {
        var widgetId = widgetContext.Id;
        if (_runningWidgets.TryGetValue(widgetId, out var widget))
        {
            widget.Activate(widgetContext);
        }
    }

    public void DeactivateWidget(string widgetId)
    {
        if (_runningWidgets.TryGetValue(widgetId, out var widget))
        {
            widget.Deactivate(widgetId);
        }
    }

    #endregion

    #region IWidgetGroupSetting

    public BaseWidgetSettings GetDefaultSettings(string widgetType)
    {
        return widgetType switch
        {
            PerformanceWidget.Type => new PerformanceSettings(),
            NetworkWidget.Type => new NetworkSettings(),
            DiskWidget.Type => new DiskSettings(),
            _ => new BaseWidgetSettings()
        };
    }

    public FrameworkElement CreateWidgetSettingContent(IWidgetSettingContext widgetSettingContext, ResourceDictionary? resourceDictionary)
    {
        var widgetSettingId = widgetSettingContext.Id;
        var widgetType = widgetSettingContext.Type;

        if (!_widgetSettingTypeRegistry.TryGetValue(widgetType, out var value))
        {
            return new UserControl();
        }

        if (_runningWidgetSettings.TryGetValue(widgetSettingId, out var widgetSetting))
        {
            return (FrameworkElement)widgetSetting;
        }

        var factory = value;
        var widgetSettingView = factory(widgetSettingId, resourceDictionary);
        _runningWidgetSettings.TryAdd(widgetSettingId, widgetSettingView);

        return (FrameworkElement)widgetSettingView;
    }

    public void OnWidgetSettingsChanged(WidgetSettingsChangedArgs settingsChangedArgs)
    {
        var baseWidgetContext = settingsChangedArgs.WidgetContext;
        var widgetId = baseWidgetContext.Id;
        if (baseWidgetContext is IWidgetContext)
        {
            if (_runningWidgets.TryGetValue(widgetId, out var widget))
            {
                widget.OnWidgetSettingsChanged(settingsChangedArgs);
            }
        }
        else if (baseWidgetContext is IWidgetSettingContext)
        {
            if (_runningWidgetSettings.TryGetValue(widgetId, out var widgetSetting))
            {
                widgetSetting.OnWidgetSettingsChanged(settingsChangedArgs);
            }
        }
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
            PerformanceWidget.Type => WidgetInitContext.LocalizationService.GetLocalizedString("Performance_Name"),
            NetworkWidget.Type => WidgetInitContext.LocalizationService.GetLocalizedString("Network_Name"),
            DiskWidget.Type => WidgetInitContext.LocalizationService.GetLocalizedString("Disk_Name"),
            _ => string.Empty
        };
    }

    public string GetLocalizedWidgetDescription(string widgetType)
    {
        return widgetType switch
        {
            PerformanceWidget.Type => WidgetInitContext.LocalizationService.GetLocalizedString("Performance_Description"),
            NetworkWidget.Type => WidgetInitContext.LocalizationService.GetLocalizedString("Network_Description"),
            DiskWidget.Type => WidgetInitContext.LocalizationService.GetLocalizedString("Disk_Description"),
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
                foreach (var widget in _runningWidgets.Values)
                {
                    widget.Dispose();
                }
                _runningWidgets.Clear();
                _widgetTypeRegistry.Clear();
                foreach (var widgetSetting in _runningWidgetSettings.Values)
                {
                    widgetSetting.Dispose();
                }
                _runningWidgetSettings.Clear();
                _widgetSettingTypeRegistry.Clear();
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
