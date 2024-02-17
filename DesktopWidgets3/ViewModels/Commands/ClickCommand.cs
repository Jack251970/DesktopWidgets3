// Copyright (c) Microsoft Corporation
// The Microsoft Corporation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Windows.Input;

namespace DesktopWidgets3.ViewModels.Commands;

public class ClickCommand : ICommand
{
    private readonly Action _execute;

    public ClickCommand(Action execute) { _execute = execute; }

    // Occurs when changes occur that affect whether or not the command should execute.
    public event EventHandler? CanExecuteChanged;

    // Defines the method that determines whether the command can execute in its current state.
    public bool CanExecute(object? parameter) => true;

    // Defines the method to be called when the command is invoked.
    public void Execute(object? parameter) => _execute();

    public void OnCanExecuteChanged() => CanExecuteChanged?.Invoke(this, EventArgs.Empty);
}

public class ClickCommandWithParam : ICommand
{
    private readonly Action<object?> _execute;

    public ClickCommandWithParam(Action<object?> execute)
    {
        _execute = execute;
    }

    public event EventHandler? CanExecuteChanged;

    public bool CanExecute(object? parameter) => true;

    public void Execute(object? parameter) => _execute(parameter);

    public void OnCanExecuteChanged() => CanExecuteChanged?.Invoke(this, EventArgs.Empty);
}

public class WidgetParameter
{
    public WidgetType WidgetType
    {
        get; set;
    }
    public int IndexTag
    {
        get; set;
    }
}
