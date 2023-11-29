using Microsoft.UI.Dispatching;
using DesktopWidgets3.Helpers;
using Windows.UI.ViewManagement;
using DesktopWidgets3.Contracts.Services;
using Microsoft.UI.Xaml.Controls;
using DesktopWidgets3.Models.Widget;

namespace DesktopWidgets3.Views.Windows;

public sealed partial class BlankWindow : WindowEx
{
    private readonly DispatcherQueue dispatcherQueue;

    private readonly UISettings settings;

    private readonly IWidgetManagerService _widgetManagerService;
    private readonly IWidgetNavigationService _widgetNavigationService;
    private readonly IWindowSinkService _windowSinkService;

    private readonly WidgetType _widgetType;

    private bool _isEditMode;

    public BlankWindow(WidgetType widgetType)
    {
        InitializeComponent();

        Content = null;
        Title = App.GetService<IWidgetResourceService>().GetWidgetLabel(widgetType);
        _widgetType = widgetType;

        IsMaximizable = IsMaximizable = false;
        SetEditMode(false);

        // Theme change code picked from https://github.com/microsoft/WinUI-Gallery/pull/1239
        dispatcherQueue = DispatcherQueue.GetForCurrentThread();
        settings = new UISettings();
        settings.ColorValuesChanged += Settings_ColorValuesChanged; // cannot use FrameworkElement.ActualThemeChanged event

        // Load registered services
        _widgetManagerService = App.GetService<IWidgetManagerService>();
        _widgetNavigationService = App.GetService<IWidgetNavigationService>();
        _windowSinkService = App.GetService<IWindowSinkService>();
    }

    // this handles updating the caption button colors correctly when indows system theme is changed while the app is open
    private void Settings_ColorValuesChanged(UISettings sender, object args)
    {
        // This calls comes off-thread, hence we will need to dispatch it to current app's thread
        dispatcherQueue.TryEnqueue(TitleBarHelper.ApplySystemThemeToCaptionButtons);
    }

    public void InitializePage(Frame? frame, object? parameter = null, bool clearNavigation = false)
    {
        // Navigate to widget page
        _widgetNavigationService.Frame = frame;
        _widgetNavigationService.InitializePage(_widgetType, parameter, clearNavigation);

        // Sink window to desktop
        _windowSinkService.Initialize(this, true);
    }

    public void SetEditMode(bool isEditMode)
    {
        IsResizable = isEditMode;
        _isEditMode = isEditMode;
    }

    /*protected override void OnPositionChanged(PointInt32 position)
    {
        if (_isEditMode)
        {
            _widgetManagerService.UpdateWidgetPosition(_widgetType, position);
        }
        base.OnPositionChanged(position);
    }

    protected override bool OnSizeChanged(Size newSize)
    {
        if (_isEditMode)
        {
            _widgetManagerService.UpdateWidgetSize(_widgetType, newSize);
        }
        return base.OnSizeChanged(newSize);
    }*/
}
