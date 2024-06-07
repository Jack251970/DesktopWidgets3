// Copyright (c) 2024 Files Community
// Licensed under the MIT License. See the LICENSE.

namespace Files.App.Actions;

internal sealed class RunAsAdminAction(IContentPageContext context) : BaseRunAsAction(context, "runas")
{
	public override string Label
		=> "RunAsAdministrator".GetLocalizedResource();

	public override string Description
		=> "RunAsAdminDescription".GetLocalizedResource();

	public override RichGlyph Glyph
		=> new("\uE7EF");
}
