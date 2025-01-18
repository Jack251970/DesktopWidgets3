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
    public WidgetViewModel? _widgetViewModel = null;

    public WidgetWindowViewModel()
    {

    }

    public void InitializeWidgetViewmodel(WidgetViewModel? widgetViewModel)
    {
        if (WidgetViewModel == null && widgetViewModel != null)
        {
            widgetViewModel.PropertyChanged += WidgetViewModel_PropertyChanged;

            WidgetViewModel = widgetViewModel;
        }
    }

    private void WidgetViewModel_PropertyChanged(object? sender, PropertyChangedEventArgs e)
    {
        switch (e.PropertyName)
        {
            case nameof(WidgetViewModel.WidgetFrameworkElement)
                when WidgetViewModel != null:
                WidgetFrameworkElement = WidgetViewModel.WidgetFrameworkElement;
                break;
        }
    }
}
