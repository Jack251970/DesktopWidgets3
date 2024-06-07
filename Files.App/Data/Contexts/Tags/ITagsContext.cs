// Copyright (c) 2024 Files Community
// Licensed under the MIT License. See the LICENSE.

namespace Files.App.Data.Contexts;

internal interface ITagsContext: INotifyPropertyChanged
{
	IEnumerable<(string path, bool isFolder)> TaggedItems { get; }
}
