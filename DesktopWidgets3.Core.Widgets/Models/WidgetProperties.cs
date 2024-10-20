using Microsoft.UI.Xaml;

namespace DesktopWidgets3.Core.Widgets.Models;

public class WidgetProperties
{
    // Id
    public static readonly DependencyProperty IdProperty =
        DependencyProperty.RegisterAttached("Id", typeof(string), typeof(WidgetProperties), new PropertyMetadata(StringUtils.GetRandomWidgetId()));

    public static string GetId(DependencyObject obj)
    {
        return (string)obj.GetValue(IdProperty);
    }

    public static void SetId(DependencyObject obj, string value)
    {
        obj.SetValue(IdProperty, value);
    }

    // Type
    public static readonly DependencyProperty TypeProperty =
        DependencyProperty.RegisterAttached("Type", typeof(string), typeof(WidgetProperties), new PropertyMetadata(string.Empty));

    public static string GetType(DependencyObject obj)
    {
        return (string)obj.GetValue(TypeProperty);
    }

    public static void SetType(DependencyObject obj, string value)
    {
        obj.SetValue(TypeProperty, value);
    }

    // Index
    public static readonly DependencyProperty IndexProperty =
        DependencyProperty.RegisterAttached("Index", typeof(int), typeof(WidgetProperties), new PropertyMetadata(-1));

    public static int GetIndex(DependencyObject obj)
    {
        return (int)obj.GetValue(IndexProperty);
    }

    public static void SetIndex(DependencyObject obj, int value)
    {
        obj.SetValue(IndexProperty, value);
    }
}
