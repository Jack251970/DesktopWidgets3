// Copyright (c) 2024 Files Community
// Licensed under the MIT License. See the LICENSE.

using System.Collections.Frozen;

namespace Files.App.Data.Commands;

internal sealed class ModifiableCommandManager : IModifiableCommandManager
{
    /*private readonly ICommandManager Commands = DependencyExtensions.GetRequiredService<ICommandManager>();*/

    private FrozenDictionary<CommandCodes, IRichCommand> ModifiableCommands = null!;

	public IRichCommand this[CommandCodes code] => ModifiableCommands.TryGetValue(code, out var command) ? command : None;

	public IRichCommand None => ModifiableCommands[CommandCodes.None];
	public IRichCommand PasteItem => ModifiableCommands[CommandCodes.PasteItem];
	public IRichCommand DeleteItem => ModifiableCommands[CommandCodes.DeleteItem];

	public ModifiableCommandManager()
	{
		/*ModifiableCommands = CreateModifiableCommands();*/
	}

    public void Initialize(IFolderViewViewModel folderViewViewModel)
    {
        ModifiableCommands = CreateModifiableCommands(folderViewViewModel.GetRequiredService<ICommandManager>());
    }

    IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();
    public IEnumerator<IRichCommand> GetEnumerator() => (ModifiableCommands.Values as IEnumerable<IRichCommand>).GetEnumerator();

    private static FrozenDictionary<CommandCodes, IRichCommand> CreateModifiableCommands(ICommandManager commands) => new Dictionary<CommandCodes, IRichCommand>
	{
		[CommandCodes.None] = new NoneCommand(),
		[CommandCodes.PasteItem] = new ModifiableCommand(commands.PasteItem, new() {
			{ KeyModifiers.Shift,  commands.PasteItemToSelection }
		}),
		[CommandCodes.DeleteItem] = new ModifiableCommand(commands.DeleteItem, new() {
			{ KeyModifiers.Shift,  commands.DeleteItemPermanently }
		}),
	}.ToFrozenDictionary();
}

