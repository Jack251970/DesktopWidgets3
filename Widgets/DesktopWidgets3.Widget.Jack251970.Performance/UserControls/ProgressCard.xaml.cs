using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;

namespace DesktopWidgets3.Widget.Jack251970.Performance.UserControls;

// TODO: Use in Disk widget.
public sealed partial class ProgressCard : UserControl
{
    public static readonly DependencyProperty LeftTitleProperty =
        DependencyProperty.Register("LeftTitle", typeof(string), typeof(ProgressCard), new PropertyMetadata(null));

    public string LeftTitle
    {
        get => (string)GetValue(LeftTitleProperty);
        set => SetValue(LeftTitleProperty, value);
    }

    public static readonly DependencyProperty RightTitleProperty =
        DependencyProperty.Register("RightTitle", typeof(string), typeof(ProgressCard), new PropertyMetadata(null));

    public string RightTitle
    {
        get => (string)GetValue(RightTitleProperty);
        set => SetValue(RightTitleProperty, value);
    }

    public static readonly DependencyProperty ProgressValueProperty =
        DependencyProperty.Register("ProgressValue", typeof(double), typeof(ProgressCard), new PropertyMetadata(null));

    public double ProgressValue
    {
        get => (double)GetValue(ProgressValueProperty);
        set => SetValue(ProgressValueProperty, value);
    }

    public ProgressCard()
    {
        InitializeComponent();
    }
}
