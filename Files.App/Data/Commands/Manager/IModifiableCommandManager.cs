// Copyright (c) 2024 Files Community
// Licensed under the MIT License. See the LICENSE.

namespace Files.App.Data.Commands;

public interface IModifiableCommandManager : IEnumerable<IRichCommand>
{
    void Initialize(IFolderViewViewModel folderViewViewModel);

    IRichCommand this[CommandCodes code] { get; }

	IRichCommand None { get; }

	IRichCommand PasteItem { get; }
	IRichCommand DeleteItem { get; }
}
