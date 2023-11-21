namespace DesktopWidgets3.Contracts.Services;

public interface ISubPageService
{
    List<string> ParentPages
    {
        get;
    }

    string GetParentPage(Type subPage);
}