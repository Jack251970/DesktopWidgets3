﻿// Copyright (c) 2023 Files Community
// Licensed under the MIT License. See the LICENSE.

using System.ComponentModel;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Input;
using System.Windows.Input;
using Microsoft.UI.Xaml;

namespace DesktopWidgets3.Files.App.Data.Commands;

public interface IRichCommand : ICommand, INotifyPropertyChanging, INotifyPropertyChanged
{
    CommandCodes Code
    {
        get;
    }

    string Label
    {
        get;
    }

    /*string LabelWithHotKey
    {
        get;
    }*/

    string AutomationName
    {
        get;
    }

    string Description
    {
        get;
    }

    RichGlyph Glyph
    {
        get;
    }

    object? Icon
    {
        get;
    }

    FontIcon? FontIcon
    {
        get;
    }

    Style? OpacityStyle
    {
        get;
    }

    /*bool IsCustomHotKeys
    {
        get;
    }

    string? HotKeyText
    {
        get;
    }

    HotKeyCollection HotKeys
    {
        get; set;
    }*/

    bool IsToggle
    {
        get;
    }

    bool IsOn
    {
        get; set;
    }

    bool IsExecutable
    {
        get;
    }

    Task ExecuteAsync();
    void ExecuteTapped(object sender, TappedRoutedEventArgs e);

    // void ResetHotKeys();
}
