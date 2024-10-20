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
