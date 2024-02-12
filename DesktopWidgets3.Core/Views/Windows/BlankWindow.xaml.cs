using Windows.UI.ViewManagement;

namespace DesktopWidgets3.Core.Views.Windows;

public sealed partial class BlankWindow : WindowEx
{
    private readonly UISettings settings;

    public Action<UISettings, object>? settings_ColorValuesChanged;

    public BlankWindow()
    {
        InitializeComponent();

        Content = null;

        settings = new UISettings();
        settings.ColorValuesChanged += Settings_ColorValuesChanged;
    }

    private void Settings_ColorValuesChanged(UISettings sender, object args)
    {
        settings_ColorValuesChanged?.Invoke(sender, args);
    }
}
