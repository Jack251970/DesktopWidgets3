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

    public async void InitializeWidgetViewmodel(WidgetViewModel? widgetViewModel)
    {
        if (WidgetViewModel == null && widgetViewModel != null)
        {
            widgetViewModel.PropertyChanged += WidgetViewModel_PropertyChanged;
            WidgetViewModel = widgetViewModel;
            await widgetViewModel.RenderAsync();
        }
    }

    private void WidgetViewModel_PropertyChanged(object? sender, PropertyChangedEventArgs e)
    {
        switch (e.PropertyName)
        {
            case nameof(WidgetViewModel.WidgetFrameworkElement):
                WidgetFrameworkElement = WidgetViewModel!.WidgetFrameworkElement;
                break;
        }
    }
}
