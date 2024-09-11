using Microsoft.UI.Xaml;

namespace DesktopWidgets3.Core.Widgets.Models;

public class WidgetProperties
{
    // Editable
    public static readonly DependencyProperty EditableProperty =
        DependencyProperty.RegisterAttached("Editable", typeof(bool), typeof(WidgetProperties), new PropertyMetadata(false));

    public static bool GetEditable(DependencyObject obj)
    {
        return (bool)obj.GetValue(EditableProperty);
    }

    public static void SetEditable(DependencyObject obj, bool value)
    {
        obj.SetValue(EditableProperty, value);
    }

    // Widget Id
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
