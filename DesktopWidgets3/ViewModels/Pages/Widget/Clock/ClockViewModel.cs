﻿using CommunityToolkit.Mvvm.ComponentModel;
using DesktopWidgets3.Contracts.Services;
using Microsoft.UI.Dispatching;

namespace DesktopWidgets3.ViewModels.Pages.Widget.Clock;

public partial class ClockViewModel : ObservableRecipient
{
    [ObservableProperty]
    private string _systemTime = string.Empty;

    private readonly string timingFormat = string.Empty;

    private readonly DispatcherQueue _dispatcherQueue = App.MainWindow!.DispatcherQueue;

    public ClockViewModel(ITimersService timersService)
    {
        timingFormat = "T";
        SystemTime = DateTime.Now.ToString(timingFormat);
        timersService.AddUpdateTimeTimerAction(UpdateTime);
    }

    private void UpdateTime(object? sender, EventArgs e)
    {
        _dispatcherQueue.TryEnqueue(() => SystemTime = DateTime.Now.ToString(timingFormat));
    }
}
