// Copyright (c) 2023 Files Community
// Licensed under the MIT License. See the LICENSE.

using DesktopWidgets3.ViewModels.Pages.Widget;
using Files.Core.Storage.LocatableStorage;

namespace Files.Core.Services;

public interface INetworkDrivesService
{
	/// <summary>
	/// Enumerates network storage devices
	/// </summary>
	/// <returns>A collection of network storage devices</returns>
	IAsyncEnumerable<ILocatableFolder> GetDrivesAsync();

    /// <summary>
    /// Displays the operating system dialog for connecting to a network storage device
    /// </summary>
    /// <param name="viewModel">The view model of the folder view that the dialog is being opened from</param>
    /// <returns></returns>
    Task OpenMapNetworkDriveDialogAsync(FolderViewViewModel viewModel);

    /// <summary>
    /// Disconnects an existing network storage device
    /// </summary>
    /// <param name="drive">An item representing the network storage device to disconnect from</param>
    /// <returns>True or false to indicate status</returns>
    bool DisconnectNetworkDrive(ILocatableFolder drive);
}
