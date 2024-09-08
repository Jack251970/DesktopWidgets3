using Microsoft.UI.Xaml;

namespace DesktopWidgets3.Models.Widget;

internal class WidgetProperties
{
    // Widget Id
    // TODO: Check if the length is correct.
    public static readonly DependencyProperty IdProperty =
        DependencyProperty.RegisterAttached("Id", typeof(string), typeof(WidgetProperties), new PropertyMetadata(Guid.NewGuid().ToString()));

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
