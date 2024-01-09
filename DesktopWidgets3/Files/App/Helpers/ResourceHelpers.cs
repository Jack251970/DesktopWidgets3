// Copyright (c) 2023 Files Community
// Licensed under the MIT License. See the LICENSE.

using DesktopWidgets3.Helpers;
using Microsoft.UI.Xaml.Markup;

namespace DesktopWidgets3.Files.App.Helpers;

[MarkupExtensionReturnType(ReturnType = typeof(string))]
public sealed class ResourceString : MarkupExtension
{
	public string Name { get; set; } = string.Empty;

	protected override object ProvideValue() => Name.GetLocalized();
}
