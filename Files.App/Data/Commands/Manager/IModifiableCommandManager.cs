// Copyright (c) 2023 Files Community
// Licensed under the MIT License. See the LICENSE.

namespace Files.App.Data.Commands;

public interface IModifiableCommandManager : IEnumerable<IRichCommand>
{
    void Initialize(ICommandManager commands);

    IRichCommand this[CommandCodes code] { get; }

	IRichCommand None { get; }

	IRichCommand PasteItem { get; }
	IRichCommand DeleteItem { get; }
}
