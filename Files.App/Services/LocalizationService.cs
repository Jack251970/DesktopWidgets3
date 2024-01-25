// Copyright (c) 2023 Files Community
// Licensed under the MIT License. See the LICENSE.

namespace Files.App.Services;

// TODO: change to internal.
public sealed class LocalizationService : ILocalizationService
{
	public string LocalizeFromResourceKey(string resourceKey)
	{
		return resourceKey.ToLocalized();
	}
}
