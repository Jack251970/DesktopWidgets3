using Microsoft.UI.Dispatching;
using DesktopWidgets3.Helpers;
using Windows.UI.ViewManagement;
using DesktopWidgets3.Contracts.Services;
using DesktopWidgets3.Models.Widget;
using Microsoft.UI.Xaml;
using Windows.Graphics;

namespace DesktopWidgets3.Views.Windows;

public sealed partial class WidgetWindow : WindowEx
{
    public PointInt32 Position
    {
        get => AppWindow.Position;
        set => WindowExtensions.Move(this, value.X, value.Y);
    }

    public WidgetSize Size
    {
        get => new(AppWindow.Size.Width * 96f / WindowExtensions.GetDpiForWindow(this), AppWindow.Size.Height * 96f / WindowExtensions.GetDpiForWindow(this));
        set => WindowExtensions.SetWindowSize(this, value.Width, value.Height);
    }

    public WidgetSize MinSize
    {
        get => new(MinWidth, MinHeight);
        set
        {
            MinWidth = value.Width;
            MinHeight = value.Height;
        }
    }

    public WidgetType WidgetType { get; }

    public int IndexTag { get; }

    private readonly DispatcherQueue dispatcherQueue;

    private readonly UISettings settings;

    private readonly IWindowSinkService _windowSinkService;

    public WidgetWindow(WidgetType widgetType, int indexTag)
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

    public void InitializeTitleBar()
    {
        IsTitleBarVisible = IsMaximizable = IsMaximizable = false;
    }
}
