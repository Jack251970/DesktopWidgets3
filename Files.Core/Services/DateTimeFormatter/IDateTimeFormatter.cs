// Copyright (c) 2023 Files Community
// Licensed under the MIT License. See the LICENSE.

namespace Files.Core.Services.DateTimeFormatter;

public interface IDateTimeFormatter
{
    public void Initialize(IFolderViewViewModel folderViewViewModel);

    string Name { get; }

	string ToShortLabel(DateTimeOffset offset);
	string ToLongLabel(DateTimeOffset offset);

	ITimeSpanLabel ToTimeSpanLabel(DateTimeOffset offset, GroupByDateUnit unit);
}
