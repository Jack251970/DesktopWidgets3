// Copyright (c) 2024 Files Community
// Licensed under the MIT License. See the LICENSE.

namespace Files.App.Data.Parameters;

#pragma warning disable IL2057 // Unrecognized value passed to the typeName parameter of 'System.Type.GetType(String)'

public sealed class CustomTabViewItemParameter
{
    // CHECK: Required is just for final checking.
    public /*required*/ IFolderViewViewModel FolderViewViewModel { get; set; } = null!;

    public Type InitialPageType { get; set; } = null!;

	public object NavigationParameter { get; set; } = null!;

    public string Serialize()
	{
        // CHANGE: Remove folder view view model from parameter of serialization.
        var tempArgs = new CustomTabViewItemParameterJson() { InitialPageType = InitialPageType, NavigationParameter = NavigationParameter };
        if (tempArgs.NavigationParameter is PaneNavigationArguments args)
        {
            tempArgs.NavigationParameter = PaneNavigationArguments.ToJson(args);
        }

        return tempArgs.Serialize();
	}

    public static CustomTabViewItemParameter Deserialize(IFolderViewViewModel folderViewViewModel, string obj)
	{
        // CHANGE: Add folder view view model from parameter of deserialization.
        var tabArgs = new CustomTabViewItemParameter
        {
            FolderViewViewModel = folderViewViewModel
        };

        var tempArgs = CustomTabViewItemParameterJson.Deserialize(obj);
        tabArgs.InitialPageType = tempArgs.InitialPageType;

        tabArgs.NavigationParameter = tempArgs.NavigationParameter;
        if (tabArgs.NavigationParameter is PaneNavigationArgumentsJson args)
        {
            tabArgs.NavigationParameter = PaneNavigationArguments.FromJson(folderViewViewModel, args);
        }

        return tabArgs;
	}
}

public sealed class CustomTabViewItemParameterJson
{
    private static readonly KnownTypesConverter _typesConverter = new();

    public Type InitialPageType { get; set; } = null!;

    public object NavigationParameter { get; set; } = null!;

    public string Serialize()
    {
        return JsonSerializer.Serialize(this, _typesConverter.Options);
    }

    public static CustomTabViewItemParameterJson Deserialize(string obj)
    {
        var tabArgs = new CustomTabViewItemParameterJson();

        var tempArgs = JsonSerializer.Deserialize<Dictionary<string, JsonElement>>(obj);
        tabArgs.InitialPageType = Type.GetType(tempArgs![nameof(InitialPageType)].GetString()!)!;

        try
        {
            tabArgs.NavigationParameter = JsonSerializer.Deserialize<PaneNavigationArgumentsJson>(tempArgs[nameof(NavigationParameter)].GetRawText())!;
        }
        catch (JsonException)
        {
            tabArgs.NavigationParameter = tempArgs[nameof(NavigationParameter)].GetString()!;
        }

        return tabArgs;
    }
}
