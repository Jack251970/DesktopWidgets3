﻿// Copyright(c) 2023 Files Community
// Licensed under the MIT License. See the LICENSE.

using DesktopWidgets3.Helpers;
using Microsoft.UI.Xaml;

namespace DesktopWidgets3.Files.App.ViewModels.Properties;

/// <summary>
/// This class is used for grouping file properties into sections so that it can be used as a grouped ListView data source
/// </summary>
public class FilePropertySection : List<FileProperty>
{
    public FilePropertySection(IEnumerable<FileProperty> items)
        : base(items)
    {
    }

    public Visibility Visibility
    {
        get; set;
    }

    public string Key
    {
        get; set;
    }

    // FILESTODO: check the resources?
    public string Title => Key.GetLocalized();

    public int Priority => sectionPriority.TryGetValue(Key, out var value) ? value : 0;

    /// <summary>
    /// This list sets the priorities for the sections
    /// </summary>
    private readonly Dictionary<string, int> sectionPriority = new()
    {
        // Core should always be last
        {"PropertySectionCore", 1}
    };
}
