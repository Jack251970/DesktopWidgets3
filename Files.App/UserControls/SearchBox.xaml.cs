// Copyright (c) 2023 Files Community
// Licensed under the MIT License. See the LICENSE.

using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Input;

namespace Files.App.UserControls;

public sealed partial class SearchBox : UserControl
{
	public static readonly DependencyProperty SearchBoxViewModelProperty =
		DependencyProperty.Register(nameof(SearchBoxViewModel), typeof(SearchBoxViewModel), typeof(SearchBox), new PropertyMetadata(null));

	public SearchBoxViewModel SearchBoxViewModel
	{
		get => (SearchBoxViewModel)GetValue(SearchBoxViewModelProperty);
        set => SetValue(SearchBoxViewModelProperty, value);
    }

    // CHANGE: Fix suggestions binding bug.
    public ObservableCollection<SuggestionModel>? Suggestions => SearchBoxViewModel?.Suggestions;

    // CHANGE: Fix query binding bug.
    public string Query
    {
        get => SearchBoxViewModel?.Query ?? string.Empty;
        set => SearchBoxViewModel.Query = value;
    }

    public SearchBox()
    {
        InitializeComponent();
    }

    // CHANGE: Fix got focus event binding bug.
    private void SearchRegion_GotFocus(object sender, RoutedEventArgs e)
        => SearchBoxViewModel?.SearchRegion_GotFocus(sender, e);

    private void SearchRegion_TextChanged(AutoSuggestBox sender, AutoSuggestBoxTextChangedEventArgs e)
		=> SearchBoxViewModel.SearchRegion_TextChanged(sender, e);

	private void SearchRegion_QuerySubmitted(AutoSuggestBox sender, AutoSuggestBoxQuerySubmittedEventArgs e)
		=> SearchBoxViewModel.SearchRegion_QuerySubmitted(sender, e);

	private void SearchRegion_Escaped(KeyboardAccelerator sender, KeyboardAcceleratorInvokedEventArgs e)
		=> SearchBoxViewModel.SearchRegion_Escaped(sender, e);

	private void SearchRegion_KeyDown(object sender, KeyRoutedEventArgs e)
		=> SearchBoxViewModel.SearchRegion_KeyDown(sender, e);
}
