using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Navigation;

using DesktopWidgets3.Contracts.Services;
using DesktopWidgets3.Contracts.ViewModels;
using DesktopWidgets3.Helpers;

namespace DesktopWidgets3.Services;

public class SubNavigationService : ISubNavigationService
{
    private readonly ISubPageService _subPageService;
    private readonly Dictionary<string, object?> _lastParameters = new();
    private readonly Dictionary<string, Frame?> _frames = new();

    public event NavigatedEventHandler? Navigated;

    public SubNavigationService(ISubPageService subPageService)
    {
        _subPageService = subPageService;

        foreach (var parentPage in _subPageService.ParentPages)
        {
            _lastParameters.Add(parentPage, null);
            _frames.Add(parentPage, null);
        }
    }

    public Frame? GetFrame(Type parentPage)
    {
        return _frames[parentPage.FullName!];
    }

    public void SetFrame(Type parentPage, Frame frame)
    {
        UnregisterFrameEvents(parentPage);
        
        _frames[parentPage.FullName!] = frame;

        RegisterFrameEvents(parentPage);
    }

    private void RegisterFrameEvents(Type parentPage)
    {
        var frame = _frames[parentPage.FullName!];

        if (frame != null)
        {
            frame.Navigated += OnNavigated;
        }
    }

    private void UnregisterFrameEvents(Type parentPage)
    {
        var frame = _frames[parentPage.FullName!];

        if (frame != null)
        {
            frame.Navigated -= OnNavigated;
        }
    }

    public bool NavigateTo(Type pageType, object? parameter = null, bool clearNavigation = true)
    {
        var parentPage = _subPageService.GetParentPage(pageType);
        var frame = _frames[parentPage];

        if (frame == null)
        {
            return false;
        }

        var lastParameter = _lastParameters[parentPage];
        var currentPageType = frame.Content?.GetType();

        if (currentPageType != pageType || (parameter != null && !parameter.Equals(lastParameter)))
        {
            frame.Tag = clearNavigation;
            var vmBeforeNavigation = frame.GetPageViewModel();
            var navigated = frame.Navigate(pageType, parameter);
            if (navigated)
            {
                _lastParameters[parentPage] = parameter;
                if (vmBeforeNavigation is INavigationAware navigationAware)
                {
                    navigationAware.OnNavigatedFrom();
                }
            }

            return navigated;
        }

        return false;
    }

    private void OnNavigated(object sender, NavigationEventArgs e)
    {
        if (sender is Frame frame)
        {
            var clearNavigation = (bool)frame.Tag;

            if (clearNavigation)
            {
                frame.BackStack.Clear();
            }

            if (frame.GetPageViewModel() is INavigationAware navigationAware)
            {
                navigationAware.OnNavigatedTo(e.Parameter);
            }

            Navigated?.Invoke(sender, e);
        }
    }

    public void InitializeDefaultPage(Type pageType, object? parameter = null, bool clearNavigation = true)
    {
        var parentPage = _subPageService.GetParentPage(pageType);
        var frame = _frames[parentPage];

        if (frame != null)
        {
            NavigateTo(pageType);
        }
    }
}
