// Copyright (c) 2023 Files Community
// Licensed under the MIT License. See the LICENSE.

using DesktopWidgets3.Helpers;
using Files.Core.Services;

namespace Files.App.Services;

internal sealed class LocalizationService : ILocalizationService
{
	public string LocalizeFromResourceKey(string resourceKey)
	{
		return resourceKey.GetLocalized();
	}
}
