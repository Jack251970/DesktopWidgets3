// Copyright (c) 2024 Files Community
// Licensed under the MIT License. See the LICENSE.

namespace Files.App.Helpers;

public sealed class WqlEventQuery(string queryExpression)
{
    public string QueryExpression { get; } = queryExpression;
}
