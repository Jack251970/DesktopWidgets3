// Copyright (c) 2024 Files Community
// Licensed under the MIT License. See the LICENSE.

namespace Files.App.Data.Contexts;

public interface IWindowContext : INotifyPropertyChanged
{
    void Initialize(IFolderViewViewModel folderViewViewModel);

    bool IsCompactOverlay { get; }
}
