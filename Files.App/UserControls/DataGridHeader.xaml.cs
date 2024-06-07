// Copyright (c) 2024 Files Community
// Licensed under the MIT License. See the LICENSE.

using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using System.Runtime.CompilerServices;
using System.Windows.Input;

namespace Files.App.UserControls;

public sealed partial class DataGridHeader : UserControl, INotifyPropertyChanged
{
    public ICommand Command { get; set; } = null!;
	public object CommandParameter { get; set; } = null!;

    private string header = null!;

	public string Header
    {
        get => header;
        set
        {
            if (value != header)
            {
                header = value;
                NotifyPropertyChanged(nameof(Header));
            }
        }
    }

    private bool canBeSorted = true;

	public bool CanBeSorted
    {
        get => canBeSorted;
        set
        {
            if (value != canBeSorted)
            {
                canBeSorted = value;
                NotifyPropertyChanged(nameof(CanBeSorted));
            }
        }
    }

    private SortDirection? columnSortOption;

	public SortDirection? ColumnSortOption
    {
        get => columnSortOption;
        set
        {
            if (value != columnSortOption)
            {
                switch (value)
                {
                    case SortDirection.Ascending:
                        VisualStateManager.GoToState(this, "SortAscending", true);
                        break;

                    case SortDirection.Descending:
                        VisualStateManager.GoToState(this, "SortDescending", true);
                        break;

                    default:
                        VisualStateManager.GoToState(this, "Unsorted", true);
                        break;
                }
                columnSortOption = value;
            }
        }
    }

    public DataGridHeader()
	{
		InitializeComponent();
	}

	public event PropertyChangedEventHandler? PropertyChanged;

	private void NotifyPropertyChanged([CallerMemberName] string propertyName = "")
	{
		PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
	}
}