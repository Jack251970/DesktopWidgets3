using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;

namespace DesktopWidgets3.Widget.Jack251970.Clock;

public partial class Main : IWidgetGroup, IWidgetGroupSetting, IWidgetLocalization, IDisposable
{
    private static readonly Dictionary<string, WidgetCreateDelegate> _widgetTypeRegistry = [];

    private readonly Dictionary<string, IWidgetViewBase> _runningWidgets = [];

    private static readonly Dictionary<string, WidgetSettingCreateDelegate> _widgetSettingTypeRegistry = [];

    private readonly Dictionary<string, IWidgetSettingViewBase> _runningWidgetSettings = [];

    #region Constructor

    public Main()
    {
        // Register widgets
        _widgetTypeRegistry.Add(DigitalClockWidget.Type, (widgetId, resourceDictionary) => new DigitalClockWidget(widgetId, resourceDictionary));
        _widgetTypeRegistry.Add(AnalogClockWidget.Type, (widgetId, resourceDictionary) => new AnalogClockWidget(widgetId, resourceDictionary));

        // Register widget settings
        _widgetSettingTypeRegistry.Add(DigitalClockWidget.Type, (widgetId, resourceDictionary) => new DigitalClockSetting(widgetId, resourceDictionary));
        _widgetSettingTypeRegistry.Add(AnalogClockWidget.Type, (widgetId, resourceDictionary) => new AnalogClockSetting(widgetId, resourceDictionary));
    }

    #endregion

    #region IWidgetGroup

    public static IWidgetInitContext WidgetInitContext => widgetInitContext;
    private static IWidgetInitContext widgetInitContext = null!;

    public void InitWidgetGroup(IWidgetInitContext widgetInitContext)
    {
        Main.widgetInitContext = widgetInitContext;
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
        var widgetView = factory(widgetId, resourceDictionary);
        _runningWidgets.Add(widgetId, widgetView);

        return (FrameworkElement)widgetView;
    }

    public void UnpinWidget(string widgetId, BaseWidgetSettings widgetSettings)
    {
        if (_runningWidgets.TryGetValue(widgetId, out var widget))
        {
            widget.Dispose();
            _runningWidgets.Remove(widgetId);
        }
    }

    public void DeleteWidget(string widgetId, BaseWidgetSettings widgetSettings)
    {
        if (_runningWidgets.TryGetValue(widgetId, out var widget))
        {
            widget.Dispose();
            _runningWidgets.Remove(widgetId);
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
        _runningWidgetSettings.Add(widgetSettingId, widgetSettingView);

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
            DigitalClockWidget.Type => WidgetInitContext.LocalizationService.GetLocalizedString("DigitalClock_Name"),
            AnalogClockWidget.Type => WidgetInitContext.LocalizationService.GetLocalizedString("AnalogClock_Name"),
            _ => string.Empty
        };
    }

    public string GetLocalizedWidgetDescription(string widgetType)
    {
        return widgetType switch
        {
            DigitalClockWidget.Type => WidgetInitContext.LocalizationService.GetLocalizedString("DigitalClock_Description"),
            AnalogClockWidget.Type => WidgetInitContext.LocalizationService.GetLocalizedString("AnalogClock_Description"),
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
                foreach (var widgetSetting in _runningWidgetSettings.Values)
                {
                    widgetSetting.Dispose();
                }
                _runningWidgetSettings.Clear();
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
