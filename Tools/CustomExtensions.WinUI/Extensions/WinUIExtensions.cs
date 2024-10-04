using System.Runtime.CompilerServices;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls.Primitives;
using WinRT;

namespace CustomExtensions.WinUI.Extensions;

public static class WinUIExtensions
{
    public static void LoadComponent<T>(this T component, ref bool contentLoaded, [CallerFilePath] string callerFilePath = "") where T : IWinRTObject
    {
        if (contentLoaded)
        {
            return;
        }

        contentLoaded = true;

        var resourceLocator = ApplicationExtensionHost.Current.LocateResource(component, callerFilePath);
        Application.LoadComponent(component, resourceLocator, ComponentResourceLocation.Nested);
    }
}
