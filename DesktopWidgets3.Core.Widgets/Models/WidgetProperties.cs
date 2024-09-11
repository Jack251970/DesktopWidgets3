using Microsoft.UI.Xaml;

namespace DesktopWidgets3.Core.Widgets.Models;

public class WidgetProperties
{
    // Is Unknown
    public static readonly DependencyProperty IsUnknownProperty =
        DependencyProperty.RegisterAttached("IsUnknown", typeof(bool), typeof(WidgetProperties), new PropertyMetadata(false));

    public static bool GetIsUnknown(DependencyObject obj)
    {
        return (bool)obj.GetValue(IsUnknownProperty);
    }

    public static void SetIsUnknown(DependencyObject obj, bool value)
    {
        obj.SetValue(IsUnknownProperty, value);
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

    // Is Preinstalled
    public static readonly DependencyProperty IsPreinstalledProperty =
        DependencyProperty.RegisterAttached("IsPreinstalled", typeof(bool), typeof(WidgetProperties), new PropertyMetadata(false));

    public static bool GetIsPreinstalled(DependencyObject obj)
    {
        return (bool)obj.GetValue(IsPreinstalledProperty);
    }

    public static void SetIsPreinstalled(DependencyObject obj, bool value)
    {
        obj.SetValue(IsPreinstalledProperty, value);
    }
}
