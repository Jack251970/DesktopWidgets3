// Copyright (c) 2024 Files Community
// Licensed under the MIT License. See the LICENSE.

namespace Files.App.Data.Contracts;

public interface IActionsSettingsService : IBaseSettingsService, INotifyPropertyChanged
{
	/// <summary>
	/// A dictionary to determine the custom hotkeys
	/// </summary>
	List<ActionWithParameterItem>? ActionsV2 { get; set; }
}
