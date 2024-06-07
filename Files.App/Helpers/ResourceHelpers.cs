// Copyright (c) 2024 Files Community
// Licensed under the MIT License. See the LICENSE.

using Microsoft.UI.Xaml.Markup;

namespace Files.App.Helpers;

[MarkupExtensionReturnType(ReturnType = typeof(string))]
public sealed class ResourceString : MarkupExtension
{
	public string Name { get; set; } = string.Empty;

	protected override object ProvideValue() => Name.GetLocalized("FilesResources");
}
