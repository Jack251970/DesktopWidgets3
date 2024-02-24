using Microsoft.UI.Xaml;

namespace DesktopWidgets3.Core.Extensions;

public class ThemeExtensions
{
    public static ElementTheme RootTheme { get; set; } = ElementTheme.Default;

    public static Action<object, ElementTheme>? ElementTheme_Changed { get; set; }
}
