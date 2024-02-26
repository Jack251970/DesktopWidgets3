// Copyright (c) 2023 Files Community
// Licensed under the MIT License. See the LICENSE.

using Microsoft.UI.Xaml.Controls;

namespace Files.App.ViewModels.Widgets;

#pragma warning disable CA1816 // Dispose methods should call SuppressFinalize

public class WidgetItem : ObservableObject, IDisposable
{
	private readonly Action<bool> _expanderValueChangedCallback;
	private readonly Func<bool> _expanderValueRequestedCallback;

	private object _WidgetControl = null!;
	public object WidgetControl
	{
		get => _WidgetControl;
		set => SetProperty(ref _WidgetControl, value);
	}

	public bool IsExpanded
	{
		get => _expanderValueRequestedCallback?.Invoke() ?? true;
		set
		{
			_expanderValueChangedCallback?.Invoke(value);
			OnPropertyChanged();
		}
	}

    public IWidgetItem WidgetItemModel => (IWidgetItem)WidgetControl;

    public string WidgetAutomationProperties => WidgetItemModel.AutomationProperties;

    public bool ShowMenuFlyout => WidgetItemModel.ShowMenuFlyout;

    public MenuFlyoutItem MenuFlyoutItem => WidgetItemModel.MenuFlyoutItem;

    public WidgetItem(object widgetControl, Action<bool> expanderValueChangedCallback, Func<bool> expanderValueRequestedCallback)
	{
		WidgetControl = widgetControl;
		_expanderValueChangedCallback = expanderValueChangedCallback;
		_expanderValueRequestedCallback = expanderValueRequestedCallback;
	}

	public void Dispose()
	{
		(WidgetControl as IDisposable)?.Dispose();
	}
}
