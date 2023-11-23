namespace DesktopWidgets3.Models;

public class DashboardWidgetItem
{
    private bool _isEnabled;

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

    public WidgetType Tag
    {
        get; set;
    }

    public bool IsEnabled
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
