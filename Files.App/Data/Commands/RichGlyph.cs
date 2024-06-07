// Copyright (c) 2024 Files Community
// Licensed under the MIT License. See the LICENSE.

using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Media;

namespace Files.App.Data.Commands;

public readonly struct RichGlyph(string baseGlyph = "", string fontFamily = "", string opacityStyle = "")
{
	public static RichGlyph None { get; } = new(string.Empty);

    public bool IsNone { get; } = string.IsNullOrEmpty(baseGlyph) && string.IsNullOrEmpty(fontFamily) && string.IsNullOrEmpty(opacityStyle);

    public string BaseGlyph { get; } = baseGlyph;
    public string FontFamily { get; } = fontFamily;
    public string OpacityStyle { get; } = opacityStyle;

    public void Deconstruct(out string baseGlyph, out string fontFamily, out string opacityStyle)
	{
		baseGlyph = BaseGlyph;
		fontFamily = FontFamily;
		opacityStyle = OpacityStyle;
	}

    public object? ToIcon()
    {
        return (object?)ToOpacityIcon() ?? ToFontIcon();
    }

    public FontIcon? ToFontIcon()
	{
		if (IsNone)
        {
            return null;
        }

        var fontIcon = new FontIcon
		{
			Glyph = BaseGlyph
		};

		if (!string.IsNullOrEmpty(FontFamily))
        {
            fontIcon.FontFamily = (FontFamily)Application.Current.Resources[FontFamily];
        }

        return fontIcon;
	}

	public OpacityIcon? ToOpacityIcon()
	{
		if (string.IsNullOrEmpty(OpacityStyle))
        {
            return null;
        }

        return new()
		{
			Style = (Style)Application.Current.Resources[OpacityStyle]
		};
	}

	public Style? ToOpacityStyle()
	{
		if (string.IsNullOrEmpty(OpacityStyle))
        {
            return null;
        }

        return (Style)Application.Current.Resources[OpacityStyle];
	}
}
