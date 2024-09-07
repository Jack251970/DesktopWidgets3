using Clock.Extensions;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using System.ComponentModel;
using System.Runtime.CompilerServices;

namespace Clock.UserControls;

public sealed partial class AnalogClock : UserControl, INotifyPropertyChanged
{
    public static readonly DependencyProperty SizeProperty =
        DependencyProperty.Register("Size", typeof(double), typeof(AnalogClock), new PropertyMetadata(null));

    public double Size
    {
        get => (double)GetValue(SizeProperty);
        set => SetValue(SizeProperty, value);
    }

    public static readonly DependencyProperty DateTimeProperty =
        DependencyProperty.Register("DateTime", typeof(DateTime), typeof(AnalogClock), new PropertyMetadata(null, OnDateTimeChanged));

    public DateTime DateTime
    {
        get => (DateTime)GetValue(DateTimeProperty);
        set => SetValue(DateTimeProperty, value);
    }

    private DateTime lastDateTime;
    public DateTime LastDateTime
    {
        get => lastDateTime;
        private set => lastDateTime = value;
    }

    private static void OnDateTimeChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
    {
        // Handle time change here, update hour, minute, second accordingly
        if (d is AnalogClock clock && e.NewValue is DateTime dateTime)
        {
            clock.UpdateHands(dateTime);
        }
    }

    public void UpdateHands(DateTime dateTime)
    {
        if (dateTime.Equals(LastDateTime, EqualsMode.Time))
        {
            return;
        }

        switch (HandsMode)
        {
            case HandsMode.Precise:
                var hour = dateTime.Hour;
                var minute = dateTime.Minute;
                var second = dateTime.Second;

                SecondValue = second;
                MinuteValue = minute * 60 + second;
                HourValue = hour * 3600 + minute * 60 + second;
                break;
            case HandsMode.Normal:
                hour = dateTime.Hour;
                minute = dateTime.Minute;
                second = dateTime.Second;

                SecondValue = second;
                MinuteValue = minute * 60;
                HourValue = hour * 3600 + minute * 60;
                break;
            default:
                hour = dateTime.Hour;
                minute = dateTime.Minute;
                second = dateTime.Second;

                SecondValue = second;
                MinuteValue = minute * 60;
                HourValue = hour * 3600;
                break;
        }

        LastDateTime = DateTime;
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

    public static readonly DependencyProperty HandsModeProperty =
        DependencyProperty.Register("HandsMode", typeof(HandsMode), typeof(AnalogClock), new PropertyMetadata(HandsMode.Precise));

    public HandsMode HandsMode
    {
        get => (HandsMode)GetValue(HandsModeProperty);
        set => SetValue(HandsModeProperty, value);
    }

    public AnalogClock()
    {
        InitializeComponent();
    }

    #region INotifyPropertyChanged

    public event PropertyChangedEventHandler? PropertyChanged;

    private void NotifyPropertyChanged([CallerMemberName] string propertyName = "")
    {
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
    }

    #endregion
}

public enum HandsMode
{
    Precise,
    Normal,
    Fast
}
