// Copyright (c) 2023 Files Community
// Licensed under the MIT License. See the LICENSE.

using CommunityToolkit.Mvvm.ComponentModel;

namespace Files.App.Data.Models;

public class AppModel : ObservableObject
{
    public AppModel()
    {
        
    }

    private string googleDrivePath = string.Empty;
    /// <summary>
    /// Gets or sets a value indicating the path for Google Drive.
    /// </summary>
    public string GoogleDrivePath
    {
        get => googleDrivePath;
        set => SetProperty(ref googleDrivePath, value);
    }

    private string pCloudDrivePath = string.Empty;
    /// <summary>
    /// Gets or sets a value indicating the path for pCloud Drive.
    /// </summary>
    public string PCloudDrivePath
    {
        get => pCloudDrivePath;
        set => SetProperty(ref pCloudDrivePath, value);
    }
}