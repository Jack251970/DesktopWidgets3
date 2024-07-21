// Copyright (c) 2024 Files Community
// Licensed under the MIT License. See the LICENSE.

/*namespace Files.App.Actions;

internal sealed class NextTabAction : ObservableObject, IAction
{
	private readonly IMultitaskingContext multitaskingContext;

	public string Label
		=> "NextTab".GetLocalizedResource();

	public string Description
		=> "NextTabDescription".GetLocalizedResource();

	public bool IsExecutable
		=> multitaskingContext.TabCount > 1;

	public HotKey HotKey
		=> new(Keys.Tab, KeyModifiers.Ctrl);

	public NextTabAction()
	{
		multitaskingContext = FolderViewViewModel.GetService<IMultitaskingContext>();

		multitaskingContext.PropertyChanged += MultitaskingContext_PropertyChanged;
    }

    public async Task ExecuteAsync(object? parameter = null)
	{
        FolderViewViewModel.TabStripSelectedIndex = (FolderViewViewModel.TabStripSelectedIndex + 1) % multitaskingContext.TabCount;

        // Small delay for the UI to load
        await Task.Delay(500);

        // Refocus on the file list
        (multitaskingContext.CurrentTabItem.TabItemContent as Control)?.Focus(FocusState.Programmatic);
    }

	private void MultitaskingContext_PropertyChanged(object? sender, PropertyChangedEventArgs e)
	{
		if (e.PropertyName == nameof(IMultitaskingContext.TabCount))
        {
            OnPropertyChanged(nameof(IsExecutable));
        }
    }
}*/
