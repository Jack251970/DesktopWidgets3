// Copyright (c) 2024 Files Community
// Licensed under the MIT License. See the LICENSE.

using Microsoft.UI.Xaml;

namespace Files.App.Converters;

internal sealed class MultiBooleanConverter
{
	public static bool OrConvert(bool a, bool b)
		=> (a || b);

	public static bool AndConvert(bool a, bool b)
		=> (a && b);

	public static bool AndNotConvert(bool a, bool b)
		=> (a && !b);

	public static bool OrAndConvert(bool a, bool b, bool c)
		=> (a || b) && c;

	public static Visibility OrConvertToVisibility(bool a, bool b)
		=> (a || b) ? Visibility.Visible : Visibility.Collapsed;

	public static Visibility NorConvertToVisibility(bool a, bool b)
		=> !(a || b) ? Visibility.Visible : Visibility.Collapsed;

	public static Visibility OrNotConvertToVisibility(bool a, bool b)
		=> OrConvertToVisibility(a, !b);
}
