﻿using Microsoft.UI.Xaml;

namespace DesktopWidgets3.Activation;

internal class DefaultActivationHandler(INavigationService navigationService) : ActivationHandler<LaunchActivatedEventArgs>
{
    private readonly INavigationService _navigationService = navigationService;

    protected override bool CanHandleInternal(LaunchActivatedEventArgs args)
    {
        // None of the ActivationHandlers has handled the activation.
        return _navigationService.Frame?.Content == null;
    }

    protected async override Task HandleInternalAsync(LaunchActivatedEventArgs args)
    {
        // Initialize to home page.
        _navigationService.NavigateTo(typeof(HomePageViewModel).FullName!, args.Arguments);

        await Task.CompletedTask;
    }
}
