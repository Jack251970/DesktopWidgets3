// Copyright (c) 2023 Files Community
// Licensed under the MIT License. See the LICENSE.

namespace Files.App.Data.EventArguments;

public class NavigationArguments
{
    public bool FocusOnNavigation { get; set; } = false;

    public string? NavPathParam { get; set; } = null;

    public bool IsSearchResultPage { get; set; } = false;

    public string? SearchPathParam { get; set; } = null;

    public string? SearchQuery { get; set; } = null;

    public bool SearchUnindexedItems { get; set; } = false;

    public bool IsLayoutSwitch { get; set; } = false;

    public IEnumerable<string>? SelectItems { get; set; } = null;

    public required bool PushFolderPath { get; set; } = false;

    public required RefreshBehaviours RefreshBehaviour { get; set; } = RefreshBehaviours.None;

    public enum RefreshBehaviours
    {
        None,
        RefreshItems,
        NavigateToPath,
    }
}