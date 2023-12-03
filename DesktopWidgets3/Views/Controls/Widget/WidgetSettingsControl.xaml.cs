using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;

namespace DesktopWidgets3.Views.Controls.Widget;

public sealed partial class SettingsControl : UserControl
{
    public SettingsControl()
    {
        InitializeComponent();
    }

    public string SettingsTitle
    {
        get => (string)GetValue(SettingsTitleProperty);
        set => SetValue(SettingsTitleProperty, value);
    }

    public string SettingsDescription
    {
        get => (string)GetValue(SettingsDescriptionProperty);
        set => SetValue(SettingsDescriptionProperty, value);
    }

    public string SettingsImageSource
    {
        get => (string)GetValue(SettingsImageSourceProperty);
        set => SetValue(SettingsImageSourceProperty, value);
    }

    public object SettingsContent
    {
        get => GetValue(SettingsContentProperty);
        set => SetValue(SettingsContentProperty, value);
    }

    public static readonly DependencyProperty SettingsTitleProperty = DependencyProperty.Register("SettingsTitle", typeof(string), typeof(SettingsControl), new PropertyMetadata(default(string)));
    public static readonly DependencyProperty SettingsDescriptionProperty = DependencyProperty.Register("SettingsDescription", typeof(string), typeof(SettingsControl), new PropertyMetadata(default(string)));
    public static readonly DependencyProperty SettingsImageSourceProperty = DependencyProperty.Register("SettingsImageSource", typeof(string), typeof(SettingsControl), new PropertyMetadata(default(string)));
    public static readonly DependencyProperty SettingsContentProperty = DependencyProperty.Register("SettingsContent", typeof(object), typeof(SettingsControl), new PropertyMetadata(new Grid()));
}