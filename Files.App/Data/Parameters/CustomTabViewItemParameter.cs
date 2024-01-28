// Copyright (c) 2023 Files Community
// Licensed under the MIT License. See the LICENSE.

namespace Files.App.Data.Parameters;

public sealed class CustomTabViewItemParameter
{
	/*private static readonly KnownTypesConverter _typesConverter = new();*/

    public IFolderViewViewModel FolderViewViewModel { get; set; } = null!;

    public Type InitialPageType { get; set; } = null!;

	public object NavigationParameter { get; set; } = null!;

    /*public string Serialize()
	{
		return JsonSerializer.Serialize(this, _typesConverter.Options);
	}

	public static CustomTabViewItemParameter Deserialize(string obj)
	{
		var tabArgs = new CustomTabViewItemParameter();

		var tempArgs = JsonSerializer.Deserialize<Dictionary<string, JsonElement>>(obj);
		tabArgs.InitialPageType = Type.GetType(tempArgs[nameof(InitialPageType)].GetString());

		try
		{
			tabArgs.NavigationParameter = JsonSerializer.Deserialize<PaneNavigationArguments>(tempArgs[nameof(NavigationParameter)].GetRawText());
		}
		catch (JsonException)
		{
			tabArgs.NavigationParameter = tempArgs[nameof(NavigationParameter)].GetString();
		}

		return tabArgs;
	}*/
}
