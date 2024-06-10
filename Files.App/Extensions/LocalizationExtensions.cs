﻿// Copyright (c) 2024 Files Community
// Licensed under the MIT License. See the LICENSE.

namespace Files.App.Extensions;

/// <summary>
/// Provides static extension for localization.
/// </summary>
public static class LocalizationExtensions
{
	private static ILocalizationService? FallbackLocalizationService;

	public static string ToLocalized(this string resourceKey, ILocalizationService? localizationService = null)
	{
		if (localizationService is null)
		{
			FallbackLocalizationService ??= DependencyExtensions.GetRequiredService<ILocalizationService>();

			return FallbackLocalizationService?.LocalizeFromResourceKey(resourceKey) ?? string.Empty;
		}

		return localizationService.LocalizeFromResourceKey(resourceKey);
	}
}
