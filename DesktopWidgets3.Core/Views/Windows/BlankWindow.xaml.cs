using Microsoft.UI.Dispatching;
using Microsoft.UI.Xaml;

using Windows.UI.ViewManagement;

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
