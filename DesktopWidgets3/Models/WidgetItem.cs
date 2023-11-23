using Newtonsoft.Json.Linq;

namespace DesktopWidgets3.Models;

public class BaseWidgetItem
{
    protected bool _isEnabled;

    public bool IsEnabled
    {
        get => _isEnabled;
        set
        {
            if (_isEnabled != value)
            {
                _isEnabled = value;
            }
        }
    }

    public WidgetType Type
    {
        get;
        set;
    }
}

public class JsonWidgetItem : BaseWidgetItem
{
    public new string Type
    {
        get => base.Type.ToString();
        set => base.Type = (WidgetType)Enum.Parse(typeof(WidgetType), value);
    }
}

public class DashboardWidgetItem : BaseWidgetItem
{
    public string? Label
    {
        get; set;
    }

    public string? Description
    {
        get; set;
    }

    public string? Icon
    {
        get; set;
    }

    public new bool IsEnabled
    {
        get => _isEnabled;
        set
        {
            if (_isEnabled != value)
            {
                _isEnabled = value;
                EnabledChangedCallback?.Invoke(this);
            }
        }
    }

    public Action<DashboardWidgetItem>? EnabledChangedCallback
    {
        get; set;
    }
}

public static class WidgetItemUtils
{
    public static JsonWidgetItem ConvertToJsonWidgetItem(DashboardWidgetItem dashboardWidgetItem)
    {
        return new JsonWidgetItem()
        {
            Type = dashboardWidgetItem.Type.ToString(),
            IsEnabled = dashboardWidgetItem.IsEnabled,
        };
    }

    public static BaseWidgetItem ConvertToBaseWidgetItem(JsonWidgetItem jsonWidgetItem)
    {
        return new BaseWidgetItem()
        {
            Type = (WidgetType)Enum.Parse(typeof(WidgetType), jsonWidgetItem.Type),
            IsEnabled = jsonWidgetItem.IsEnabled,
        };
    }
}
