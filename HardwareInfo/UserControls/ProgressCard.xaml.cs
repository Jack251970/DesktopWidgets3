using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;

// To learn more about WinUI, the WinUI project structure,
// and more about our project templates, see: http://aka.ms/winui-project-info.

namespace HardwareInfo.UserControls;

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

