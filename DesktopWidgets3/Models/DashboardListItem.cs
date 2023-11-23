using System.ComponentModel;
using System.Runtime.CompilerServices;

namespace DesktopWidgets3.Models;

public class DashboardListItem // : INotifyPropertyChanged
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
                // OnPropertyChanged();
                EnabledChangedCallback?.Invoke(this);
            }
        }
    }

    public Action<DashboardListItem>? EnabledChangedCallback
    {
        get; set;
    }

    /*public event PropertyChangedEventHandler? PropertyChanged;

    private void OnPropertyChanged([CallerMemberName] string? propertyName = null)
    {
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
    }*/
}
