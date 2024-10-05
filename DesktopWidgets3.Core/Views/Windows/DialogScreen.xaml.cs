using Windows.UI.Composition;
using WinUIEx;

namespace DesktopWidgets3.Core.Views.Windows;

/// <summary>
/// An empty window that can be used for showing message dialog on full screen.
/// </summary>
public sealed partial class DialogScreen : FullScreen
{
    public DialogScreen()
    {
        InitializeComponent();

        Title = string.Empty;

        SystemBackdrop = new BlurredBackdrop();

        SystemHelper.HideWindowIconFromTaskbar(this.GetWindowHandle());
    }

    #region Backdrop

    private partial class BlurredBackdrop : CompositionBrushBackdrop
    {
        protected override CompositionBrush CreateBrush(Compositor compositor)
            => compositor.CreateHostBackdropBrush();
    }

    #endregion
}
