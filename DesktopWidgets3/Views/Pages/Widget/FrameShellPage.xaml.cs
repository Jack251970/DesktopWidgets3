using Microsoft.UI.Xaml.Controls;

using DesktopWidgets3.ViewModels.Pages.Widget;

namespace DesktopWidgets3.Views.Pages.Widget;

public sealed partial class FrameShellPage : Page
{
    public FrameNavShellViewModel ViewModel
    {
        get;
    }

    public FrameShellPage(FrameNavShellViewModel viewModel)
    {
        ViewModel = viewModel;
        InitializeComponent();
    }
}
