// Copyright (c) 2024 Files Community
// Licensed under the MIT License. See the LICENSE.

namespace Files.App.Data.EventArguments;

public sealed class SearchBoxQuerySubmittedEventArgs(SuggestionModel chosenSuggestion)
{
    public SuggestionModel ChosenSuggestion { get; } = chosenSuggestion;
}
