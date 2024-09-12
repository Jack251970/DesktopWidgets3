using Microsoft.UI.Xaml;

namespace DesktopWidgets3.Core.Views.Windows;

public sealed partial class BlankWindowEx : WindowEx
{
    public UIElement? TitleBar { get; set; }

    public UIElement? TitleBarText { get; set; }

    public BlankWindowEx()
    {
        InitializeComponent();

        Content = null;
    }
}
