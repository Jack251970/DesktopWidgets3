// Copyright (c) 2024 Files Community
// Licensed under the MIT License. See the LICENSE.

using DesktopWidgets3.Core.Helpers;
using LiteDB;
using Microsoft.Win32;
using System.IO;
using System.Runtime.CompilerServices;
using static Files.App.Helpers.LayoutPreferencesDatabaseItemRegistry;
using static Files.App.Helpers.RegistryHelpers;
using JsonSerializer = System.Text.Json.JsonSerializer;

namespace Files.App.Helpers;

public sealed class LayoutPreferencesDatabase
{
    // CHANGE: Use InfoHelper instead of Package.Current.
    private static readonly string LayoutSettingsKey = @$"Software\Files Community\{InfoHelper.GetFullName()}\v1\LayoutPreferences";

    // CHANGE: Use LocalSettingsExtensions instead of Package.Current.
    private static readonly string LayoutSettingsDbPath = Path.Combine(LocalSettingsExtensions.GetApplicationDataFolder("Files"), "user_settings.db");
    private const string LayoutSettingsCollectionName = "layoutprefs";

	static LayoutPreferencesDatabase()
	{
		if (File.Exists(LayoutSettingsDbPath))
		{
			using (var database = new LiteDatabase(new ConnectionString(LayoutSettingsDbPath)
			{
				Connection = ConnectionType.Direct,
				Upgrade = true
			}))
			{
				ImportCore(database.GetCollection<LayoutPreferencesDatabaseItem>(LayoutSettingsCollectionName).FindAll().ToArray());
			}

			File.Delete(LayoutSettingsDbPath);
		}
	}

	public LayoutPreferencesItem? GetPreferences(IFolderViewViewModel folderViewViewModel, string filePath, ulong? frn)
	{
		return FindPreferences(folderViewViewModel, filePath, frn)?.LayoutPreferencesManager;
	}

	public void SetPreferences(IFolderViewViewModel folderViewViewModel, string filePath, ulong? frn, LayoutPreferencesItem? preferencesItem)
	{
		var tmp = FindPreferences(folderViewViewModel, filePath, frn);

		if (tmp is null)
		{
			if (preferencesItem is not null)
			{
				// Insert new tagged file (Id will be auto-incremented)
				var newPref = new LayoutPreferencesDatabaseItem(folderViewViewModel)
				{
					FilePath = filePath,
					Frn = frn,
					LayoutPreferencesManager = preferencesItem
				};

				UpdateValues(newPref);
			}
		}
		else
		{
			if (preferencesItem is not null)
			{
				// Update file tag
				tmp.LayoutPreferencesManager = preferencesItem;

				UpdateValues(tmp);
			}
			else
			{
				// Remove file tag
				UpdateValues(null);
			}
		}

		void UpdateValues(LayoutPreferencesDatabaseItem? preferences)
		{
			if (filePath is not null)
			{
				using var filePathKey = Registry.CurrentUser.CreateSubKey(CombineKeys(LayoutSettingsKey, filePath));
				SaveValues(filePathKey, preferences!);
			}

			if (frn is not null)
			{
				using var frnKey = Registry.CurrentUser.CreateSubKey(CombineKeys(LayoutSettingsKey, "FRN", frn.Value.ToString()));
				SaveValues(frnKey, preferences!);
			}
		}
	}

	public void ResetAll()
	{
		Registry.CurrentUser.DeleteSubKeyTree(LayoutSettingsKey, false);
	}

	public void Import(string json)
	{
		var preferences = JsonSerializer.Deserialize<LayoutPreferencesDatabaseItem[]>(json);
		ImportCore(preferences);
	}

	private static void ImportCore(LayoutPreferencesDatabaseItem[]? preferences)
	{
		Registry.CurrentUser.DeleteSubKeyTree(LayoutSettingsKey, false);
		if (preferences is null)
		{
			return;
		}
		foreach (var preference in preferences)
		{
			using var filePathKey = Registry.CurrentUser.CreateSubKey(CombineKeys(LayoutSettingsKey, preference.FilePath));
			SaveValues(filePathKey, preference);
			if (preference.Frn is not null)
			{
				using var frnKey = Registry.CurrentUser.CreateSubKey(CombineKeys(LayoutSettingsKey, "FRN", preference.Frn.Value.ToString()));
				SaveValues(frnKey, preference);
			}
		}
	}

	public string Export(IFolderViewViewModel folderViewViewModel)
	{
		var list = new List<LayoutPreferencesDatabaseItem>();
		IterateKeys(folderViewViewModel, list, LayoutSettingsKey, 0);
		return JsonSerializer.Serialize(list);
	}

	private void IterateKeys(IFolderViewViewModel folderViewViewModel, List<LayoutPreferencesDatabaseItem> list, string path, int depth)
	{
		using var key = Registry.CurrentUser.OpenSubKey(path);
		if (key is null)
		{
			return;
		}

		if (key.ValueCount > 0)
		{
			var preference = new LayoutPreferencesDatabaseItem(folderViewViewModel);
			BindValues(key, preference);
			list.Add(preference);
		}

		foreach (var subKey in key.GetSubKeyNames())
		{
			if (depth == 0 && subKey == "FRN")
			{
				// Skip FRN key
				continue;
			}

			IterateKeys(folderViewViewModel, list, CombineKeys(path, subKey), depth + 1);
		}
	}

	private LayoutPreferencesDatabaseItem? FindPreferences(IFolderViewViewModel folderViewViewModel, string filePath, ulong? frn)
	{
		if (filePath is not null)
		{
			using var filePathKey = Registry.CurrentUser.CreateSubKey(CombineKeys(LayoutSettingsKey, filePath));
			if (filePathKey.ValueCount > 0)
			{
				var preference = new LayoutPreferencesDatabaseItem(folderViewViewModel);
				BindValues(filePathKey, preference);
				if (frn is not null)
				{
					// Keep entry updated
					preference.Frn = frn;
					var value = frn.Value;
					filePathKey.SetValue(nameof(LayoutPreferencesDatabaseItem.Frn), Unsafe.As<ulong, long>(ref value), RegistryValueKind.QWord);
				}
				return preference;
			}
		}

		if (frn is not null)
		{
			using var frnKey = Registry.CurrentUser.CreateSubKey(CombineKeys(LayoutSettingsKey, "FRN", frn.Value.ToString()));
			if (frnKey.ValueCount > 0)
			{
				var preference = new LayoutPreferencesDatabaseItem(folderViewViewModel);
				BindValues(frnKey, preference);
				if (filePath is not null)
				{
					// Keep entry updated
					preference.FilePath = filePath;
					frnKey.SetValue(nameof(LayoutPreferencesDatabaseItem.FilePath), filePath, RegistryValueKind.String);
				}
				return preference;
			}
		}

		return null;
	}
}
