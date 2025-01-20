using Microsoft.UI.Xaml.Markup;

namespace DesktopWidgets3.Widget.Jack251970.SystemInfo.Helpers;

[MarkupExtensionReturnType(ReturnType = typeof(string))]
public sealed partial class ResourceHelper : MarkupExtension
{
    public string Name { get; set; } = string.Empty;

    protected override object ProvideValue() => Main.WidgetInitContext.LocalizationService.GetLocalizedString(Name);
}
