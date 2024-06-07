// Copyright (c) 2024 Files Community
// Licensed under the MIT License. See the LICENSE.

namespace Files.App.Helpers;

/// <summary>
/// Represents manager for the database of layout preferences.
/// </summary>
public class LayoutPreferencesDatabaseManager
{
    // Fields
    private static readonly Lazy<LayoutPreferencesDatabase> dbInstance = new(() => new());

    // Methods
    public LayoutPreferencesItem? GetPreferences(IFolderViewViewModel folderViewViewModel, string filePath, ulong? frn = null)
    {
        return dbInstance.Value.GetPreferences(folderViewViewModel, filePath, frn);
    }

    public void SetPreferences(IFolderViewViewModel folderViewViewModel, string filePath, ulong? frn, LayoutPreferencesItem? preferencesItem)
    {
        dbInstance.Value.SetPreferences(folderViewViewModel, filePath, frn, preferencesItem);
    }

    public void ResetAll()
    {
        dbInstance.Value.ResetAll();
    }

    public void Import(string json)
    {
        dbInstance.Value.Import(json);
    }

    public string Export(IFolderViewViewModel folderViewViewModel)
    {
        return dbInstance.Value.Export(folderViewViewModel);
    }
}
