// Copyright (c) 2023 Files Community
// Licensed under the MIT License. See the LICENSE.

namespace Files.App.Helpers;

public sealed class LocalizedEnumHelper<T> where T : Enum
{
	public string Name
	{
		get
		{
			var localized = $"{typeof(T).Name}_{Enum.GetName(typeof(T), Value)}".ToLocalized();

			if (string.IsNullOrEmpty(localized))
			{
				localized = $"{Enum.GetName(typeof(T), Value)}".ToLocalized();
			}

			return localized;
		}
	}

	public T Value { get; set; }

	public LocalizedEnumHelper(T value)
	{
		Value = value;
	}
}
