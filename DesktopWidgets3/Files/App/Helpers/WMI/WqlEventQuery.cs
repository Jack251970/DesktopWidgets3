// Copyright (c) 2023 Files Community
// Licensed under the MIT License. See the LICENSE.

namespace DesktopWidgets3.Files.App.Helpers;

public class WqlEventQuery
{
	public string QueryExpression { get; }

	public WqlEventQuery(string queryExpression)
	{
		QueryExpression = queryExpression;
	}
}
