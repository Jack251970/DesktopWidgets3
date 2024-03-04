using System.ComponentModel;
using System.Runtime.CompilerServices;

// To learn more about WinUI, the WinUI project structure,
// and more about our project templates, see: http://aka.ms/winui-project-info.

namespace Clock.UserControls;

public sealed partial class AnalogClock : UserControl, INotifyPropertyChanged
{
    public static readonly DependencyProperty DateTimeProperty =
        DependencyProperty.Register("DateTime", typeof(DateTime), typeof(AnalogClock), new PropertyMetadata(null, OnDateTimeChanged));

    public DateTime DateTime
    {
        get => (DateTime)GetValue(DateTimeProperty);
        set => SetValue(DateTimeProperty, value);
    }

    private static void OnDateTimeChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
    {
        // Handle time change here, update hour, minute, second accordingly
        if (d is AnalogClock clock && e.NewValue is DateTime dateTime)
        {
            var hour = dateTime.Hour;
            var minute = dateTime.Minute;
            var second = dateTime.Second;

            clock.SecondValue = second;
            clock.MinuteValue = minute * 60 + second;
            clock.HourValue = hour * 3600 + minute * 60 + second;
        }
    }

    private int hourValue = 0;

    public int HourValue
    {
        get => hourValue;
        set
        {
            if (value != hourValue)
            {
                hourValue = value;
                NotifyPropertyChanged(nameof(HourValue));
            }
        }
    }

    private int minuteValue = 0;

    public int MinuteValue
    {
        get => minuteValue;
        set
        {
            if (value != minuteValue)
            {
                minuteValue = value;
                NotifyPropertyChanged(nameof(MinuteValue));
            }
        }
    }

    private int secondValue = 0;

    public int SecondValue
    {
        get => secondValue;
        set
        {
            if (value != secondValue)
            {     
                secondValue = value;
                NotifyPropertyChanged(nameof(SecondValue));
            }
        }
    }

    public AnalogClock()
    {
        InitializeComponent();
    }

    public event PropertyChangedEventHandler? PropertyChanged;

    private void NotifyPropertyChanged([CallerMemberName] string propertyName = "")
    {
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
    }
}
