using Microsoft.UI.Xaml;

namespace DesktopWidgets3.Models.Widget;

internal class WidgetProperties
{
    // Widget Type
    public static readonly DependencyProperty WidgetTypeProperty =
        DependencyProperty.RegisterAttached("WidgetType", typeof(WidgetType), typeof(WidgetProperties), new PropertyMetadata(WidgetType.Clock));

    public static WidgetType GetWidgetType(DependencyObject obj)
    {
        return (WidgetType)obj.GetValue(WidgetTypeProperty);
    }

    public static void SetWidgetType(DependencyObject obj, WidgetType value)
    {
        obj.SetValue(WidgetTypeProperty, value);
    }

    // Index Tag
    public static readonly DependencyProperty IndexTagProperty =
        DependencyProperty.RegisterAttached("IndexTag", typeof(int), typeof(WidgetProperties), new PropertyMetadata(-1));

    public static int GetIndexTag(DependencyObject obj)
    {
        return (int)obj.GetValue(IndexTagProperty);
    }

    public static void SetIndexTag(DependencyObject obj, int value)
    {
        obj.SetValue(IndexTagProperty, value);
    }
}
