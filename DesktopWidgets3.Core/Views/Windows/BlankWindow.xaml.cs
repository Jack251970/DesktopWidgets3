using Microsoft.UI.Xaml;

namespace DesktopWidgets3.Core.Views.Windows;

public sealed partial class BlankWindow : WindowEx
{
    public UIElement? TitleBar { get; set; }

    public UIElement? TitleBarText { get; set; }

    public BlankWindow()
    {
        InitializeComponent();

        Content = null;
    }
}
