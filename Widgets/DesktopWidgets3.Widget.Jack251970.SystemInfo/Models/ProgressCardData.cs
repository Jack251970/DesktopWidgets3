using System.ComponentModel;
using System.Runtime.CompilerServices;

namespace DesktopWidgets3.Widget.Jack251970.SystemInfo.Models;

public class ProgressCardData : INotifyPropertyChanged
{
    private string leftTitle = string.Empty;

    public string LeftTitle
    {
        get => leftTitle;
        set
        {
            if (value != leftTitle)
            {
                leftTitle = value;
                NotifyPropertyChanged(nameof(LeftTitle));
            }
        }
    }

    private string rightTitle = string.Empty;

    public string RightTitle
    {
        get => rightTitle;
        set
        {
            if (value != rightTitle)
            {
                rightTitle = value;
                NotifyPropertyChanged(nameof(RightTitle));
            }
        }
    }

    private double progressValue = 0;

    public double ProgressValue
    {
        get => progressValue;
        set
        {
            if (value != progressValue)
            {
                progressValue = value;
                NotifyPropertyChanged(nameof(ProgressValue));
            }
        }
    }

    public event PropertyChangedEventHandler? PropertyChanged;

    private void NotifyPropertyChanged([CallerMemberName] string propertyName = "")
    {
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
    }
}
