using System.ComponentModel;
using CommunityToolkit.Mvvm.ComponentModel;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Media;

namespace DesktopWidgets3.Core.Widgets.ViewModels.Windows;

public partial class WidgetWindowViewModel : ObservableRecipient
{
    [ObservableProperty]
    private GridLength _headerHeight;

    [ObservableProperty]
    public Brush? _widgetIconFill = null;

    [ObservableProperty]
    public string _widgetDisplayTitle = string.Empty;

    [ObservableProperty]
    public FrameworkElement _widgetFrameworkElement = new ProgressRing();

    [ObservableProperty]
    public MenuFlyout? _widgetMenuFlyout = null;

    [ObservableProperty]
    public WidgetViewModel? _widgetSource = null;

    public WidgetWindowViewModel()
    {

    }

    partial void OnWidgetSourceChanging(WidgetViewModel? oldValue, WidgetViewModel? newValue)
    {
        if (oldValue != null)
        {
            oldValue.PropertyChanged -= WidgetSource_PropertyChanged;
        }
        if (newValue != null)
        {
            // TODO: SetScaledWidthAndHeight
            newValue.PropertyChanged += WidgetSource_PropertyChanged;
        }
    }

    private void WidgetSource_PropertyChanged(object? sender, PropertyChangedEventArgs e)
    {
        switch (e.PropertyName)
        {
            case nameof(WidgetViewModel.WidgetFrameworkElement)
                when WidgetSource != null:
                WidgetFrameworkElement = WidgetSource.WidgetFrameworkElement;
                break;
        }
    }
}
