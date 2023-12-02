using Windows.Graphics;

namespace DesktopWidgets3.Models.Widget;

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

    public WidgetType Type { get; set; }

    public int IndexTag { get; set; }
}

public class JsonWidgetItem : BaseWidgetItem
{
    public new string Type
    {
        get => base.Type.ToString();
        set => base.Type = (WidgetType)Enum.Parse(typeof(WidgetType), value);
    }

    public PointInt32 Position { get; set; }

    public WidgetSize Size { get; set; }
}

public class DashboardWidgetItem : BaseWidgetItem
{
    public string? Label { get; set; }

    public string? Description { get; set; }

    public string? Icon { get; set; }

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