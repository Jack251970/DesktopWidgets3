// Copyright (c) 2024 Files Community
// Licensed under the MIT License. See the LICENSE.

using CommunityToolkit.Mvvm.Messaging.Messages;

namespace Files.App.Data.Messages;

/// <summary>
/// Represents a messenger for FileSystemDialog option changed.
/// </summary>
/// <remarks>
/// Initializes a class.
/// </remarks>
public sealed class FileSystemDialogOptionChangedMessage(FileSystemDialogConflictItemViewModel value)
        : ValueChangedMessage<FileSystemDialogConflictItemViewModel>(value)
{
}
