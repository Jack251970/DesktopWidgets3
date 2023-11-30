using Microsoft.UI.Dispatching;
using DesktopWidgets3.Helpers;
using Windows.UI.ViewManagement;
using DesktopWidgets3.Contracts.Services;
using DesktopWidgets3.Models.Widget;
using Microsoft.UI.Xaml;

namespace DesktopWidgets3.Views.Windows;

public sealed partial class BlankWindow : WindowEx
{
    public WidgetType WidgetType { get; }

    public int IndexTag { get; }

    public UIElement? TitleBar { get; set; }

    private readonly DispatcherQueue dispatcherQueue;

    private readonly UISettings settings;

    private readonly IWindowSinkService _windowSinkService;

    public BlankWindow(WidgetType widgetType, int indexTag)
    {
        InitializeComponent();

        WidgetType = widgetType;
        IndexTag = indexTag;

        Content = null;
        Title = string.Empty;

        // Theme change code picked from https://github.com/microsoft/WinUI-Gallery/pull/1239
        dispatcherQueue = DispatcherQueue.GetForCurrentThread();
        settings = new UISettings();
        settings.ColorValuesChanged += Settings_ColorValuesChanged; // cannot use FrameworkElement.ActualThemeChanged event

        // Load registered services
        _windowSinkService = App.GetService<IWindowSinkService>();

        // Sink window to desktop
        _windowSinkService.Initialize(this, true);
    }

    // this handles updating the caption button colors correctly when indows system theme is changed while the app is open
    private void Settings_ColorValuesChanged(UISettings sender, object args)
    {
        // This calls comes off-thread, hence we will need to dispatch it to current app's thread
        dispatcherQueue.TryEnqueue(TitleBarHelper.ApplySystemThemeToCaptionButtons);
    }

    public void InitializeTitleBar(UIElement? titleBar)
    {
        TitleBar = titleBar;
        IsTitleBarVisible = IsMaximizable = IsMaximizable = false;
    }
}
