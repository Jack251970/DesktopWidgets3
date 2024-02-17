namespace DesktopWidgets3.Contracts.ViewModels;

internal interface INavigationAware
{
    void OnNavigatedTo(object parameter);

    void OnNavigatedFrom();
}
