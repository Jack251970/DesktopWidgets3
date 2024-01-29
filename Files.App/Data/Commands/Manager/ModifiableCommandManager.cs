// Copyright (c) 2023 Files Community
// Licensed under the MIT License. See the LICENSE.

using System.Collections.Immutable;

namespace Files.App.Data.Commands;

internal class ModifiableCommandManager : IModifiableCommandManager
{
    /*private readonly ICommandManager Commands = DependencyExtensions.GetService<ICommandManager>();*/

    private IImmutableDictionary<CommandCodes, IRichCommand> ModifiableCommands = null!;

	public IRichCommand this[CommandCodes code] => ModifiableCommands.TryGetValue(code, out var command) ? command : None;

	public IRichCommand None => ModifiableCommands[CommandCodes.None];
	public IRichCommand PasteItem => ModifiableCommands[CommandCodes.PasteItem];
	public IRichCommand DeleteItem => ModifiableCommands[CommandCodes.DeleteItem];

	public ModifiableCommandManager()
	{
		/*ModifiableCommands = CreateModifiableCommands().ToImmutableDictionary();*/
	}

    public void Initialize(ICommandManager commands)
    {
        ModifiableCommands = CreateModifiableCommands(commands).ToImmutableDictionary();
    }

    IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();
	public IEnumerator<IRichCommand> GetEnumerator() => ModifiableCommands.Values.GetEnumerator();

	private static IDictionary<CommandCodes, IRichCommand> CreateModifiableCommands(ICommandManager commands) => new Dictionary<CommandCodes, IRichCommand>
	{
		[CommandCodes.None] = new NoneCommand(),
		[CommandCodes.PasteItem] = new ModifiableCommand(commands.PasteItem, new() {
			{ KeyModifiers.Shift,  commands.PasteItemToSelection }
		}),
		[CommandCodes.DeleteItem] = new ModifiableCommand(commands.DeleteItem, new() {
			{ KeyModifiers.Shift,  commands.DeleteItemPermanently }
		}),
	};
}

