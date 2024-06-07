// Copyright (c) 2024 Files Community
// Licensed under the MIT License. See the LICENSE.

using CommunityToolkit.WinUI;

namespace Files.App.Utils.Storage;

public sealed class GroupedCollection<T> : BulkConcurrentObservableCollection<T>, IGroupedCollectionHeader
{
    public GroupedHeaderViewModel Model
    {
        get; set;
    } = null!;

    public GroupedCollection(IEnumerable<T> items) : base(items)
    {
        AddEvents();
    }

    public GroupedCollection(string key) : base()
    {
        AddEvents();
        Model = new GroupedHeaderViewModel()
        {
            Key = key,
            Text = key,
        };
    }

    public GroupedCollection(string key, string text) : base()
    {
        AddEvents();
        Model = new GroupedHeaderViewModel()
        {
            Key = key,
            Text = text,
        };
    }

    private void AddEvents()
    {
        PropertyChanged += GroupedCollection_PropertyChanged!;
    }

    private void GroupedCollection_PropertyChanged(object sender, PropertyChangedEventArgs e)
    {
        if (e.PropertyName == nameof(Count))
        {
            // CHANGE: Fix for null reference exception.
            if (Model.CountText is not null)
            {
                Model.CountText = string.Format(
                    Count > 1
                        ? "GroupItemsCount_Plural".GetLocalized()
                        : "GroupItemsCount_Singular".GetLocalized(),
                    Count);
            }
        }
    }

    public void InitializeExtendedGroupHeaderInfoAsync()
    {
        if (GetExtendedGroupHeaderInfo is null)
        {
            return;
        }

        Model.ResumePropertyChangedNotifications(false);

        GetExtendedGroupHeaderInfo.Invoke(this);
        Model.Initialized = true;

        if (isBulkOperationStarted)
        {
            Model.PausePropertyChangedNotifications();
        }
    }

    public override void BeginBulkOperation()
    {
        base.BeginBulkOperation();

        Model.PausePropertyChangedNotifications();
    }

    public override void EndBulkOperation()
    {
        base.EndBulkOperation();

        Model.ResumePropertyChangedNotifications();
    }
}
