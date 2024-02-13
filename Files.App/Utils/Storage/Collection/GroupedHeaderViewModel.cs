// Copyright (c) 2023 Files Community
// Licensed under the MIT License. See the LICENSE.

using Microsoft.UI.Xaml.Media;
using System.Runtime.CompilerServices;

namespace Files.App.Utils.Storage;

#pragma warning disable CS8618 // Non-nullable field must contain a non-null value when exiting constructor. Consider declaring as nullable.

public class GroupedHeaderViewModel : ObservableObject
{
    public string Key
    {
        get; set;
    }
    public bool Initialized
    {
        get; set;
    }
    public int SortIndexOverride
    {
        get; set;
    }

    private string text;

    public string Text
    {
        get => text ?? ""; // Text is bound to AutomationProperties.Name and can't be null
        set => SetPropertyWithUpdateDelay(ref text, value);
    }

    private string subtext;

    public string Subtext
    {
        get => subtext;
        set => SetPropertyWithUpdateDelay(ref subtext, value);
    }

    private string countText;

    public string CountText
    {
        get => countText;
        set => SetPropertyWithUpdateDelay(ref countText, value);
    }

    private bool showCountTextBelow;

    public bool ShowCountTextBelow
    {
        get => showCountTextBelow;
        set => SetProperty(ref showCountTextBelow, value);
    }

    private ImageSource imageSource;

    public ImageSource ImageSource
    {
        get => imageSource;
        set => SetPropertyWithUpdateDelay(ref imageSource, value);
    }

    private string icon;

    public string Icon
    {
        get => icon;
        set => SetPropertyWithUpdateDelay(ref icon, value);
    }

    private void SetPropertyWithUpdateDelay<T>(ref T field, T newVal, [CallerMemberName] string propName = null!)
    {
        if (propName is null)
        {
            return;
        }
        var name = propName.StartsWith("get_", StringComparison.OrdinalIgnoreCase)
            ? propName[4..]
            : propName;

        if (!deferPropChangedNotifs)
        {
            SetProperty(ref field, newVal, name);
        }
        else
        {
            field = newVal;
            if (!changedPropQueue.Contains(name))
            {
                changedPropQueue.Add(name);
            }
        }
    }

    public void PausePropertyChangedNotifications()
    {
        deferPropChangedNotifs = true;
    }

    public void ResumePropertyChangedNotifications(bool triggerUpdates = true)
    {
        if (deferPropChangedNotifs == false)
        {
            return;
        }
        deferPropChangedNotifs = false;
        if (triggerUpdates)
        {
            changedPropQueue.ForEach(OnPropertyChanged);
            changedPropQueue.Clear();
        }
    }

    private readonly List<string> changedPropQueue = new();

    // This is true by default to make it easier to initialize groups from a different thread
    private bool deferPropChangedNotifs = true;
}
