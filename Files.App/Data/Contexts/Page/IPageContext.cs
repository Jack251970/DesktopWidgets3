// Copyright (c) 2023 Files Community
// Licensed under the MIT License. See the LICENSE.

namespace Files.App.Data.Contexts;

public interface IPageContext
{
    void Initialize(PaneHolderPage? modifiedPage);

    event EventHandler? Changing;
	event EventHandler? Changed;

	IShellPage? Pane { get; }
	IShellPage? PaneOrColumn { get; }
}
