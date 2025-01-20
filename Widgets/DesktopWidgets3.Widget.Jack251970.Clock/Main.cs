using System.Collections.Concurrent;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Serilog;

namespace DesktopWidgets3.Widget.Jack251970.Clock;

public partial class Main : IWidgetGroup, IWidgetGroupSetting, IWidgetLocalization, IDisposable
{
    private static readonly ILogger _log = Log.ForContext("SourceContext", nameof(Main));

    public static IWidgetInitContext WidgetInitContext => widgetInitContext;
    private static IWidgetInitContext widgetInitContext = null!;

    private static readonly Dictionary<string, WidgetCreateDelegate> _widgetTypeRegistry = [];

    private readonly ConcurrentDictionary<string, IWidgetViewBase> _runningWidgets = [];

    private static readonly Dictionary<string, WidgetSettingCreateDelegate> _widgetSettingTypeRegistry = [];

    private readonly ConcurrentDictionary<string, IWidgetSettingViewBase> _runningWidgetSettings = [];

    #region Constructor

    public Main()
    {
        // Register widgets
        _widgetTypeRegistry.Add(DigitalClockWidget.Type, (widgetId) => new DigitalClockWidget(widgetId));
        _widgetTypeRegistry.Add(AnalogClockWidget.Type, (widgetId) => new AnalogClockWidget(widgetId));

        // Register widget settings
        _widgetSettingTypeRegistry.Add(DigitalClockWidget.Type, (widgetId) => new DigitalClockSetting(widgetId));
        _widgetSettingTypeRegistry.Add(AnalogClockWidget.Type, (widgetId) => new AnalogClockSetting(widgetId));
    }

    #endregion

    #region IWidgetGroup

    public void InitWidgetGroup(IWidgetInitContext widgetInitContext)
    {
        _log.Information("Initializing widget group {WidgetGroupId}", widgetInitContext.WidgetGroupMetadata.ID);

        Main.widgetInitContext = widgetInitContext;
    }

    public FrameworkElement CreateWidgetContent(IWidgetContext widgetContext)
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
        var widgetView = factory(widgetId);
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
            DigitalClockWidget.Type => new DigitalClockSettings(),
            AnalogClockWidget.Type => new AnalogClockSettings(),
            _ => new BaseWidgetSettings()
        };
    }

    public FrameworkElement CreateWidgetSettingContent(IWidgetSettingContext widgetSettingContext)
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
        var widgetSettingView = factory(widgetSettingId);
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
            DigitalClockWidget.Type => WidgetInitContext.LocalizationService.GetLocalizedString("DigitalClock.Name"),
            AnalogClockWidget.Type => WidgetInitContext.LocalizationService.GetLocalizedString("AnalogClock.Name"),
            _ => string.Empty
        };
    }

    public string GetLocalizedWidgetDescription(string widgetType)
    {
        return widgetType switch
        {
            DigitalClockWidget.Type => WidgetInitContext.LocalizationService.GetLocalizedString("DigitalClock.Description"),
            AnalogClockWidget.Type => WidgetInitContext.LocalizationService.GetLocalizedString("AnalogClock.Description"),
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
