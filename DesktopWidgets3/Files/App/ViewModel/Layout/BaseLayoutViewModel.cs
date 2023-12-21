// Copyright (c) 2023 Files Community
// Licensed under the MIT License. See the LICENSE.

using System.Windows.Input;
using CommunityToolkit.Mvvm.Input;
using Files.App.Helpers;
using Files.Core.Data.Enums;
using Files.Core.Data.Items;

namespace Files.App.ViewModels.Layouts;

/// <summary>
/// Represents ViewModel for <see cref="BaseLayoutPage"/>.
/// </summary>
public class BaseLayoutViewModel : IDisposable
{
    public ICommand CreateNewFileCommand
    {
        get; private set;
    }

    public BaseLayoutViewModel()
    {
        CreateNewFileCommand = new RelayCommand<ShellNewEntry>(CreateNewFile);
    }

    private async void CreateNewFile(ShellNewEntry f)
    {
        // await UIFilesystemHelpers.CreateFileFromDialogResultTypeAsync(AddItemDialogItemType.File, f);//_associatedInstance);
    }

    public void Dispose()
    {
        
    }
}